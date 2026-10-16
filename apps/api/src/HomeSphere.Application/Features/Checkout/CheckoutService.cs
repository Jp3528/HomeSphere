using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Checkout.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Features.Checkout;

public interface ICheckoutService
{
    Task<CheckoutPreviewResponse> GetCheckoutPreviewAsync(
        Guid? userId,
        string? guestToken,
        CheckoutPreviewRequest request,
        CancellationToken cancellationToken = default);

    Task<ValidateCouponResponse> ValidateCouponAsync(
        string code,
        decimal subtotal,
        CancellationToken cancellationToken = default);
}

public class CheckoutService : ICheckoutService
{
    private readonly IApplicationDbContext _context;

    public CheckoutService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CheckoutPreviewResponse> GetCheckoutPreviewAsync(
        Guid? userId,
        string? guestToken,
        CheckoutPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = new List<string>();

        // 1. Anti-IDOR Check on Address (if provided)
        if (request.AddressId.HasValue && userId.HasValue)
        {
            var address = await _context.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.AddressId.Value, cancellationToken);

            if (address == null)
            {
                validationErrors.Add("La dirección de entrega especificada no existe.");
            }
            else if (address.UserId != userId.Value)
            {
                // Strict Anti-IDOR protection
                throw new ForbiddenAccessException("La dirección seleccionada no pertenece a tu cuenta.");
            }
        }

        // 2. Locate Cart
        var cart = await FindCartWithItemsAsync(userId, guestToken, cancellationToken);
        if (cart == null || !cart.Items.Any())
        {
            return new CheckoutPreviewResponse(
                CartId: cart?.Id ?? Guid.Empty,
                Items: Array.Empty<CheckoutItemPreviewDto>(),
                Subtotal: 0m,
                DiscountTotal: 0m,
                AppliedCoupon: null,
                Shipping: CalculateShipping("standard", 0m),
                Total: 0m,
                CanProceedToOrder: false,
                ValidationErrors: new[] { "El carrito se encuentra vacío." }
            );
        }

        // 3. Recalculate Items against live Catalog & Stock
        var itemPreviews = new List<CheckoutItemPreviewDto>();
        bool allItemsAvailable = true;

        foreach (var item in cart.Items)
        {
            var variant = item.ProductVariant;
            var product = variant.Product;
            var unitPrice = variant.Price;
            var lineTotal = unitPrice * item.Quantity;
            var availableStock = variant.StockQuantity;

            bool isAvailable = true;
            string? errorMsg = null;
            string? warningMsg = null;

            if (!variant.IsActive || product.Status != ProductStatus.Active)
            {
                isAvailable = false;
                errorMsg = $"El producto '{product.Name} - {variant.Name}' ya no está disponible para la venta.";
                allItemsAvailable = false;
            }
            else if (availableStock <= 0)
            {
                isAvailable = false;
                errorMsg = $"El producto '{product.Name} - {variant.Name}' se encuentra actualmente agotado.";
                allItemsAvailable = false;
            }
            else if (availableStock < item.Quantity)
            {
                isAvailable = false;
                errorMsg = $"Stock insuficiente para '{product.Name} - {variant.Name}'. Solicitaste {item.Quantity} pero solo quedan {availableStock} unidades.";
                allItemsAvailable = false;
            }

            if (errorMsg != null)
            {
                validationErrors.Add(errorMsg);
            }

            itemPreviews.Add(new CheckoutItemPreviewDto(
                VariantId: variant.Id,
                Sku: variant.Sku,
                ProductName: product.Name,
                ProductSlug: product.Slug,
                VariantName: variant.Name,
                UnitPrice: unitPrice,
                Quantity: item.Quantity,
                LineTotal: lineTotal,
                AvailableStock: availableStock,
                IsAvailable: isAvailable,
                WarningMessage: warningMsg,
                ErrorMessage: errorMsg
            ));
        }

        var subtotal = itemPreviews.Sum(i => i.LineTotal);

        // 4. Shipping Calculation
        var shipping = CalculateShipping(request.ShippingMethod, subtotal);

        // 5. Coupon Evaluation
        CouponSummaryDto? appliedCoupon = null;
        decimal discountTotal = 0m;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var couponValidation = await ValidateCouponInternalAsync(request.CouponCode, subtotal, cancellationToken);
            if (couponValidation.IsValid && couponValidation.Coupon != null)
            {
                appliedCoupon = couponValidation.Coupon;
                discountTotal = couponValidation.Coupon.DiscountAmount;
            }
            else if (!string.IsNullOrWhiteSpace(couponValidation.Message))
            {
                validationErrors.Add(couponValidation.Message);
            }
        }

        var total = Math.Max(0m, subtotal - discountTotal) + shipping.Cost;
        bool canProceed = allItemsAvailable && !validationErrors.Any(e => e.Contains("agotado") || e.Contains("insuficiente") || e.Contains("no está disponible"));

        return new CheckoutPreviewResponse(
            CartId: cart.Id,
            Items: itemPreviews,
            Subtotal: subtotal,
            DiscountTotal: discountTotal,
            AppliedCoupon: appliedCoupon,
            Shipping: shipping,
            Total: total,
            CanProceedToOrder: canProceed,
            ValidationErrors: validationErrors
        );
    }

    public async Task<ValidateCouponResponse> ValidateCouponAsync(
        string code,
        decimal subtotal,
        CancellationToken cancellationToken = default)
    {
        return await ValidateCouponInternalAsync(code, subtotal, cancellationToken);
    }

    private async Task<ValidateCouponResponse> ValidateCouponInternalAsync(
        string code,
        decimal subtotal,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new ValidateCouponResponse(false, "El código de cupón no puede estar vacío.", null);
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var coupon = await _context.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);

        if (coupon == null || !coupon.IsActive)
        {
            return new ValidateCouponResponse(false, $"El cupón '{normalizedCode}' no existe o ya no es válido.", null);
        }

        var now = DateTimeOffset.UtcNow;
        if (now < coupon.ValidFromUtc)
        {
            return new ValidateCouponResponse(false, $"El cupón '{coupon.Code}' aún no se encuentra vigente.", null);
        }

        if (now > coupon.ValidUntilUtc)
        {
            return new ValidateCouponResponse(false, $"El cupón '{coupon.Code}' expiró el {coupon.ValidUntilUtc:dd/MM/yyyy}.", null);
        }

        if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value)
        {
            return new ValidateCouponResponse(false, $"El cupón '{coupon.Code}' ha alcanzado el límite máximo de canjes.", null);
        }

        if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
        {
            return new ValidateCouponResponse(
                false,
                $"El cupón '{coupon.Code}' requiere un subtotal mínimo de compra de ${coupon.MinOrderAmount.Value:N2} USD (actual: ${subtotal:N2} USD).",
                null
            );
        }

        decimal discountAmount;
        if (coupon.DiscountType == DiscountType.Percentage)
        {
            discountAmount = Math.Round(subtotal * (coupon.DiscountValue / 100m), 2);
            if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
            {
                discountAmount = coupon.MaxDiscountAmount.Value;
            }
        }
        else
        {
            discountAmount = Math.Min(subtotal, coupon.DiscountValue);
        }

        var summary = new CouponSummaryDto(
            Code: coupon.Code,
            Description: coupon.Description,
            DiscountType: coupon.DiscountType.ToString(),
            DiscountValue: coupon.DiscountValue,
            DiscountAmount: discountAmount
        );

        return new ValidateCouponResponse(true, null, summary);
    }

    private static ShippingSummaryDto CalculateShipping(string method, decimal subtotal)
    {
        var normalized = method?.Trim().ToLowerInvariant() ?? "standard";

        if (normalized == "express")
        {
            return new ShippingSummaryDto(
                Code: "express",
                Name: "Envío Express Prioritario",
                Cost: 45.00m,
                EstimatedDelivery: "Entrega asegurada en 24 a 48 horas hábiles",
                IsFree: false
            );
        }

        // Standard
        bool isFree = subtotal >= 1000.00m;
        return new ShippingSummaryDto(
            Code: "standard",
            Name: "Envío Estándar a Domicilio",
            Cost: isFree ? 0.00m : 15.00m,
            EstimatedDelivery: "Entrega de 3 a 5 días hábiles",
            IsFree: isFree
        );
    }

    private async Task<Domain.Entities.Cart?> FindCartWithItemsAsync(Guid? userId, string? guestToken, CancellationToken cancellationToken)
    {
        if (userId.HasValue)
        {
            return await _context.Carts
                .AsNoTracking()
                .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(guestToken))
        {
            return await _context.Carts
                .AsNoTracking()
                .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(c => c.GuestToken == guestToken && c.UserId == null, cancellationToken);
        }

        return null;
    }
}
