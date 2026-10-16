using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Cart.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Features.Cart;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid? userId, string? guestToken, CancellationToken cancellationToken = default);
    Task<CartDto> AddItemAsync(Guid? userId, string? guestToken, AddToCartRequest request, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateItemQuantityAsync(Guid? userId, string? guestToken, Guid itemId, int quantity, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveItemAsync(Guid? userId, string? guestToken, Guid itemId, CancellationToken cancellationToken = default);
    Task<CartDto> ClearCartAsync(Guid? userId, string? guestToken, CancellationToken cancellationToken = default);
    Task<CartDto> MergeCartAsync(Guid userId, string guestToken, CancellationToken cancellationToken = default);
}

public class CartService : ICartService
{
    private readonly IApplicationDbContext _context;

    public CartService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CartDto> GetCartAsync(Guid? userId, string? guestToken, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(userId, guestToken, cancellationToken);
        if (cart == null)
        {
            return new CartDto(
                Id: Guid.NewGuid(),
                UserId: userId,
                GuestToken: guestToken,
                Items: Array.Empty<CartItemDto>(),
                TotalItems: 0,
                Subtotal: 0m,
                CreatedAtUtc: DateTime.UtcNow,
                UpdatedAtUtc: null
            );
        }

        return MapToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(Guid? userId, string? guestToken, AddToCartRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new BusinessRuleValidationException("La cantidad debe ser mayor a cero.");
        }

        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == request.VariantId, cancellationToken);

        if (variant == null)
        {
            throw new NotFoundException("ProductVariant", request.VariantId);
        }

        if (!variant.IsActive || variant.Product.Status != ProductStatus.Active)
        {
            throw new BusinessRuleValidationException("El producto o variante seleccionado no se encuentra disponible.");
        }

        if (variant.StockQuantity <= 0)
        {
            throw new BusinessRuleValidationException("El producto seleccionado se encuentra agotado.");
        }

        var cart = await GetOrCreateCartAsync(userId, guestToken, cancellationToken);

        var existingItem = cart.Items.FirstOrDefault(i => i.ProductVariantId == request.VariantId);
        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + request.Quantity;
            if (newQuantity > 99)
            {
                newQuantity = 99;
            }

            existingItem.Quantity = newQuantity;
            existingItem.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            var initialQuantity = Math.Min(request.Quantity, 99);
            var newItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductVariantId = variant.Id,
                Quantity = initialQuantity,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.CartItems.Add(newItem);
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var reloaded = await FindCartAsync(cart.UserId, cart.GuestToken, cancellationToken);
        return MapToDto(reloaded ?? cart);
    }

    public async Task<CartDto> UpdateItemQuantityAsync(Guid? userId, string? guestToken, Guid itemId, int quantity, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(userId, guestToken, cancellationToken);
        if (cart == null)
        {
            throw new NotFoundException("Cart not found for the current session.");
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            throw new NotFoundException("CartItem", itemId);
        }

        if (quantity <= 0)
        {
            cart.Items.Remove(item);
            _context.CartItems.Remove(item);
        }
        else
        {
            item.Quantity = Math.Min(quantity, 99);
            item.UpdatedAtUtc = DateTime.UtcNow;
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(cart);
    }

    public async Task<CartDto> RemoveItemAsync(Guid? userId, string? guestToken, Guid itemId, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(userId, guestToken, cancellationToken);
        if (cart == null)
        {
            throw new NotFoundException("Cart not found for the current session.");
        }

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            cart.Items.Remove(item);
            _context.CartItems.Remove(item);
            cart.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(cart);
    }

    public async Task<CartDto> ClearCartAsync(Guid? userId, string? guestToken, CancellationToken cancellationToken = default)
    {
        var cart = await FindCartAsync(userId, guestToken, cancellationToken);
        if (cart != null && cart.Items.Any())
        {
            _context.CartItems.RemoveRange(cart.Items);
            cart.Items.Clear();
            cart.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return cart != null ? MapToDto(cart) : new CartDto(
            Id: Guid.NewGuid(),
            UserId: userId,
            GuestToken: guestToken,
            Items: Array.Empty<CartItemDto>(),
            TotalItems: 0,
            Subtotal: 0m,
            CreatedAtUtc: DateTime.UtcNow,
            UpdatedAtUtc: null
        );
    }

    public async Task<CartDto> MergeCartAsync(Guid userId, string guestToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(guestToken))
        {
            return await GetCartAsync(userId, null, cancellationToken);
        }

        var guestCart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.ProductVariant)
            .ThenInclude(v => v.Product)
            .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.GuestToken == guestToken && c.UserId == null, cancellationToken);

        if (guestCart == null || !guestCart.Items.Any())
        {
            return await GetCartAsync(userId, null, cancellationToken);
        }

        var userCart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.ProductVariant)
            .ThenInclude(v => v.Product)
            .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (userCart == null)
        {
            // Reassign guest cart directly to the authenticated user
            guestCart.UserId = userId;
            guestCart.GuestToken = null;
            guestCart.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return MapToDto(guestCart);
        }

        // Merge items from guestCart into userCart
        foreach (var guestItem in guestCart.Items)
        {
            var existingUserItem = userCart.Items.FirstOrDefault(ui => ui.ProductVariantId == guestItem.ProductVariantId);
            if (existingUserItem != null)
            {
                existingUserItem.Quantity = Math.Min(99, existingUserItem.Quantity + guestItem.Quantity);
                existingUserItem.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                var newMergedItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = userCart.Id,
                    ProductVariantId = guestItem.ProductVariantId,
                    Quantity = Math.Min(99, guestItem.Quantity),
                    CreatedAtUtc = DateTime.UtcNow
                };
                _context.CartItems.Add(newMergedItem);
            }
        }

        userCart.UpdatedAtUtc = DateTime.UtcNow;
        _context.Carts.Remove(guestCart);
        await _context.SaveChangesAsync(cancellationToken);

        var reloadedMerged = await FindCartAsync(userCart.UserId, null, cancellationToken);
        return MapToDto(reloadedMerged ?? userCart);
    }

    private async Task<Domain.Entities.Cart?> FindCartAsync(Guid? userId, string? guestToken, CancellationToken cancellationToken)
    {
        if (userId.HasValue)
        {
            return await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                .ThenInclude(v => v.Product)
                .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(guestToken))
        {
            return await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.ProductVariant)
                .ThenInclude(v => v.Product)
                .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(c => c.GuestToken == guestToken && c.UserId == null, cancellationToken);
        }

        return null;
    }

    private async Task<Domain.Entities.Cart> GetOrCreateCartAsync(Guid? userId, string? guestToken, CancellationToken cancellationToken)
    {
        var cart = await FindCartAsync(userId, guestToken, cancellationToken);
        if (cart != null)
        {
            return cart;
        }

        cart = new Domain.Entities.Cart
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GuestToken = userId.HasValue ? null : (string.IsNullOrWhiteSpace(guestToken) ? Guid.NewGuid().ToString("N") : guestToken),
            ExpiresAtUtc = userId.HasValue ? null : DateTimeOffset.UtcNow.AddDays(30),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private static CartDto MapToDto(Domain.Entities.Cart cart)
    {
        var items = cart.Items
            .OrderBy(i => i.CreatedAtUtc)
            .Select(i =>
            {
                var variant = i.ProductVariant;
                var product = variant.Product;
                var unitPrice = variant.Price;
                var lineTotal = unitPrice * i.Quantity;
                var isAvailable = variant.IsActive && product.Status == ProductStatus.Active && variant.StockQuantity >= i.Quantity;

                string? warning = null;
                if (!variant.IsActive || product.Status != ProductStatus.Active)
                {
                    warning = "Este producto ya no se encuentra disponible.";
                }
                else if (variant.StockQuantity <= 0)
                {
                    warning = "Producto agotado.";
                }
                else if (variant.StockQuantity < i.Quantity)
                {
                    warning = $"Stock disponible limitado: solo quedan {variant.StockQuantity} unidades.";
                }

                var primaryImage = product.Images?.OrderBy(img => img.DisplayOrder).FirstOrDefault()?.Url;

                return new CartItemDto(
                    Id: i.Id,
                    ProductVariantId: variant.Id,
                    Sku: variant.Sku,
                    ProductName: product.Name,
                    ProductSlug: product.Slug,
                    VariantName: variant.Name,
                    UnitPrice: unitPrice,
                    Quantity: i.Quantity,
                    LineTotal: lineTotal,
                    PrimaryImageUrl: primaryImage,
                    AttributesJson: variant.AttributesJson,
                    AvailableStock: variant.StockQuantity,
                    IsAvailable: isAvailable,
                    Warning: warning
                );
            })
            .ToList();

        var subtotal = items.Sum(i => i.LineTotal);
        var totalItems = items.Sum(i => i.Quantity);

        return new CartDto(
            Id: cart.Id,
            UserId: cart.UserId,
            GuestToken: cart.GuestToken,
            Items: items,
            TotalItems: totalItems,
            Subtotal: subtotal,
            CreatedAtUtc: cart.CreatedAtUtc,
            UpdatedAtUtc: cart.UpdatedAtUtc
        );
    }
}
