using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Addresses.DTOs;
using HomeSphere.Application.Features.Orders.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CartEntity = HomeSphere.Domain.Entities.Cart;

namespace HomeSphere.Application.Features.Orders;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentProvider _paymentProvider;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IApplicationDbContext context,
        IPaymentProvider paymentProvider,
        ILogger<OrderService> logger)
    {
        _context = context;
        _paymentProvider = paymentProvider;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderAsync(
        CreateOrderRequest request,
        Guid? userId,
        string? guestCartToken,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        // 1. Idempotency Check
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingRecord = await _context.IdempotencyRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == idempotencyKey && r.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);

            if (existingRecord != null)
            {
                _logger.LogInformation("Returning cached order response for Idempotency-Key: {Key}", idempotencyKey);
                try
                {
                    var cachedOrder = JsonSerializer.Deserialize<OrderDto>(existingRecord.ResponseBody);
                    if (cachedOrder != null)
                    {
                        return cachedOrder;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize cached response for key {Key}", idempotencyKey);
                }
            }
        }

        // 2. Resolve Shipping Address with Anti-IDOR validation
        string shippingAddressJson;
        if (request.ShippingAddressId.HasValue)
        {
            var address = await _context.Addresses
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.ShippingAddressId.Value, cancellationToken);

            if (address == null)
            {
                throw new NotFoundException("Address", request.ShippingAddressId.Value);
            }

            if (userId.HasValue && address.UserId != userId.Value)
            {
                // Strict Anti-IDOR protection
                throw new ForbiddenAccessException("La dirección de entrega especificada no pertenece a tu cuenta.");
            }

            shippingAddressJson = JsonSerializer.Serialize(new
            {
                RecipientName = address.RecipientName,
                PhoneNumber = address.PhoneNumber,
                Line1 = address.Line1,
                Line2 = address.Line2,
                District = address.District,
                City = address.City,
                Region = address.Region,
                Reference = address.Reference
            });
        }
        else if (request.GuestShippingAddress != null)
        {
            var g = request.GuestShippingAddress;
            if (string.IsNullOrWhiteSpace(g.RecipientName) || string.IsNullOrWhiteSpace(g.Line1) ||
                string.IsNullOrWhiteSpace(g.City) || string.IsNullOrWhiteSpace(g.Region))
            {
                throw new BusinessRuleValidationException("Faltan campos obligatorios en la dirección de entrega.");
            }

            shippingAddressJson = JsonSerializer.Serialize(new
            {
                RecipientName = g.RecipientName,
                PhoneNumber = g.PhoneNumber,
                Line1 = g.Line1,
                Line2 = g.Line2,
                District = g.District,
                City = g.City,
                Region = g.Region,
                Reference = g.Reference
            });
        }
        else
        {
            throw new BusinessRuleValidationException("Debes especificar una dirección de entrega válida.");
        }

        // 3. Find User's or Guest's Cart
        CartEntity? cart = null;
        if (userId.HasValue)
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value, cancellationToken);
        }

        if (cart == null && !string.IsNullOrWhiteSpace(guestCartToken))
        {
            cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.GuestToken == guestCartToken, cancellationToken);
        }

        if (cart == null || !cart.Items.Any())
        {
            throw new BusinessRuleValidationException("El carrito se encuentra vacío.");
        }

        // 4. Begin Transaction for atomic stock validation, order creation and reservation
        await using var tx = await _context.BeginTransactionAsync(cancellationToken);

        try
        {
            var orderItems = new List<OrderItem>();
            var stockReservations = new List<StockReservation>();
            decimal subtotal = 0m;

            var orderId = Guid.NewGuid();
            var orderNumber = GenerateOrderNumber();
            var publicId = $"ord_{Guid.NewGuid():N}";
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(15);

            foreach (var cartItem in cart.Items)
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .ThenInclude(p => p.Images)
                    .FirstOrDefaultAsync(v => v.Id == cartItem.ProductVariantId, cancellationToken);

                if (variant == null || !variant.IsActive || variant.Product.Status != ProductStatus.Active)
                {
                    throw new BusinessRuleValidationException(
                        $"El artículo '{variant?.Product?.Name ?? cartItem.ProductVariantId.ToString()}' ya no se encuentra disponible.");
                }

                // Inventory Concurrency Control & Stock Reservation
                if (variant.StockQuantity < cartItem.Quantity)
                {
                    throw new BusinessRuleValidationException(
                        $"Stock insuficiente para '{variant.Product.Name} - {variant.Name}' (SKU: {variant.Sku}). Stock disponible: {variant.StockQuantity}, solicitado: {cartItem.Quantity}.");
                }

                // Deduct available stock atomically
                variant.StockQuantity -= cartItem.Quantity;

                var lineTotal = variant.Price * cartItem.Quantity;
                subtotal += lineTotal;

                var primaryImage = variant.Product.Images.OrderBy(img => img.DisplayOrder).FirstOrDefault()?.Url;

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductVariantId = variant.Id,
                    SkuSnapshot = variant.Sku,
                    ProductNameSnapshot = $"{variant.Product.Name} - {variant.Name}",
                    UnitPrice = variant.Price,
                    Quantity = cartItem.Quantity,
                    TotalPrice = lineTotal,
                    AttributesJsonSnapshot = variant.AttributesJson,
                    ImageUrlSnapshot = primaryImage
                };
                orderItems.Add(orderItem);

                var reservation = new StockReservation
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductVariantId = variant.Id,
                    Quantity = cartItem.Quantity,
                    ExpiresAtUtc = expiresAtUtc,
                    Status = ReservationStatus.Pending
                };
                stockReservations.Add(reservation);
            }

            // 5. Server Authority: Calculate Shipping
            decimal shippingCost = 15.00m;
            if (string.Equals(request.ShippingMethod, "express", StringComparison.OrdinalIgnoreCase))
            {
                shippingCost = 45.00m;
            }
            else
            {
                // Standard shipping free on orders >= $1,000
                if (subtotal >= 1000m)
                {
                    shippingCost = 0.00m;
                }
            }

            // 6. Server Authority: Calculate Coupon
            decimal discountTotal = 0m;
            string? appliedCouponCode = null;

            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                var cleanCode = request.CouponCode.Trim().ToUpperInvariant();
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code == cleanCode, cancellationToken);

                if (coupon == null || !coupon.IsActive)
                {
                    throw new BusinessRuleValidationException($"El cupón '{cleanCode}' no es válido o ha expirado.");
                }

                var now = DateTimeOffset.UtcNow;
                if (coupon.ValidFromUtc > now || coupon.ValidUntilUtc < now)
                {
                    throw new BusinessRuleValidationException($"El cupón '{cleanCode}' se encuentra fuera de su periodo de vigencia.");
                }

                if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value)
                {
                    throw new BusinessRuleValidationException($"El cupón '{cleanCode}' ha alcanzado el límite máximo de usos.");
                }

                if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
                {
                    throw new BusinessRuleValidationException(
                        $"El cupón '{cleanCode}' requiere una compra mínima de ${coupon.MinOrderAmount.Value:F2}.");
                }

                if (coupon.DiscountType == DiscountType.Percentage)
                {
                    var calcDiscount = subtotal * (coupon.DiscountValue / 100m);
                    if (coupon.MaxDiscountAmount.HasValue && calcDiscount > coupon.MaxDiscountAmount.Value)
                    {
                        calcDiscount = coupon.MaxDiscountAmount.Value;
                    }
                    discountTotal = calcDiscount;
                }
                else
                {
                    discountTotal = Math.Min(coupon.DiscountValue, subtotal);
                }

                coupon.TimesUsed += 1;
                appliedCouponCode = coupon.Code;
            }

            var total = Math.Max(0m, subtotal - discountTotal + shippingCost);

            // 7. Create Order Entity
            var order = new Order
            {
                Id = orderId,
                OrderNumber = orderNumber,
                PublicId = publicId,
                UserId = userId,
                GuestEmail = request.GuestEmail,
                Status = OrderStatus.PendingPayment,
                Subtotal = subtotal,
                DiscountTotal = discountTotal,
                ShippingCost = shippingCost,
                Total = total,
                CouponCode = appliedCouponCode,
                ShippingMethod = request.ShippingMethod,
                ShippingAddressSnapshotJson = shippingAddressJson,
                CustomerNotes = request.CustomerNotes,
                Items = orderItems,
                Reservations = stockReservations,
                StatusHistory = new List<OrderStatusHistory>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        PreviousStatus = OrderStatus.PendingPayment,
                        NewStatus = OrderStatus.PendingPayment,
                        ChangedBy = userId.HasValue ? "Customer" : "Guest",
                        Reason = "Orden creada en espera de pago (reserva de 15 minutos)"
                    }
                }
            };

            _context.Orders.Add(order);

            // 8. Clean up user's cart items
            _context.CartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync(cancellationToken);

            var responseDto = MapToOrderDto(order);

            // 9. Save Idempotency Record if key provided
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var record = new IdempotencyRecord
                {
                    Key = idempotencyKey,
                    UserId = userId,
                    Endpoint = "POST /api/orders",
                    RequestHash = ComputeSha256(JsonSerializer.Serialize(request)),
                    ResponseStatusCode = 201,
                    ResponseBody = JsonSerializer.Serialize(responseDto),
                    CreatedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
                };
                _context.IdempotencyRecords.Add(record);
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (tx != null)
            {
                await tx.CommitAsync(cancellationToken);
            }

            _logger.LogInformation("Order {OrderNumber} ({PublicId}) successfully created with {ItemCount} items. Total: ${Total:F2}",
                order.OrderNumber, order.PublicId, order.Items.Count, order.Total);

            return responseDto;
        }
        catch (Exception ex)
        {
            if (tx != null)
            {
                await tx.RollbackAsync(cancellationToken);
            }
            _logger.LogError(ex, "Transaction failed while creating order. Rollback executed cleanly.");
            throw;
        }
    }

    public async Task<OrderDto> GetOrderByPublicIdAsync(
        string publicId,
        Guid? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory.OrderByDescending(h => h.CreatedAtUtc))
            .FirstOrDefaultAsync(o => o.PublicId == publicId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", publicId);
        }

        // Anti-IDOR: Check that non-admin logged-in user can only view their own orders
        if (!isAdmin && order.UserId.HasValue)
        {
            if (!userId.HasValue || order.UserId.Value != userId.Value)
            {
                throw new ForbiddenAccessException("No tienes autorización para acceder a los datos de este pedido.");
            }
        }

        return MapToOrderDto(order);
    }

    public async Task<OrderDto> GetOrderByIdAsync(
        Guid orderId,
        Guid? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory.OrderByDescending(h => h.CreatedAtUtc))
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", orderId);
        }

        if (!isAdmin && order.UserId.HasValue)
        {
            if (!userId.HasValue || order.UserId.Value != userId.Value)
            {
                throw new ForbiddenAccessException("No tienes autorización para acceder a este pedido.");
            }
        }

        return MapToOrderDto(order);
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetUserOrdersAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.OrderNumber,
                o.PublicId,
                o.Status.ToString(),
                o.Total,
                o.Items.Count,
                o.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetAllOrdersAsync(
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        return await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.OrderNumber,
                o.PublicId,
                o.Status.ToString(),
                o.Total,
                o.Items.Count,
                o.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(
        Guid orderId,
        OrderStatus newStatus,
        string changedBy,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Reservations)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", orderId);
        }

        var oldStatus = order.Status;

        if (oldStatus == newStatus)
        {
            return MapToOrderDto(order);
        }

        // Validate State Machine Transitions
        ValidateStatusTransition(oldStatus, newStatus);

        // State Machine Actions
        if (newStatus == OrderStatus.Cancelled)
        {
            order.CancelledAtUtc = DateTime.UtcNow;
            order.CancellationReason = reason ?? "Pedido cancelado";

            // Restore Stock and Release Reservations exactly once
            foreach (var reservation in order.Reservations.Where(r => r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed))
            {
                var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == reservation.ProductVariantId, cancellationToken);
                if (variant != null)
                {
                    variant.StockQuantity += reservation.Quantity;
                    _logger.LogInformation("Restored {Qty} units of stock for Variant {Sku} on Order {OrderNumber} cancellation",
                        reservation.Quantity, variant.Sku, order.OrderNumber);
                }
                reservation.Status = ReservationStatus.Released;
            }
        }
        else if (newStatus == OrderStatus.Paid)
        {
            order.PaidAtUtc = DateTime.UtcNow;

            // Confirm Reservations
            foreach (var reservation in order.Reservations.Where(r => r.Status == ReservationStatus.Pending))
            {
                reservation.Status = ReservationStatus.Confirmed;
            }
        }
        else if (newStatus == OrderStatus.Shipped)
        {
            order.ShippedAtUtc = DateTime.UtcNow;
        }
        else if (newStatus == OrderStatus.Delivered)
        {
            order.DeliveredAtUtc = DateTime.UtcNow;
        }

        order.Status = newStatus;

        var history = new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            PreviousStatus = oldStatus,
            NewStatus = newStatus,
            ChangedBy = changedBy,
            Reason = reason
        };
        _context.OrderStatusHistories.Add(history);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderNumber} status transitioned from {OldStatus} to {NewStatus} by {Actor}. Reason: {Reason}",
            order.OrderNumber, oldStatus, newStatus, changedBy, reason);

        return MapToOrderDto(order);
    }

    public async Task<OrderDto> CancelOrderAsync(
        Guid orderId,
        Guid? userId,
        bool isAdmin,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", orderId);
        }

        // Anti-IDOR check
        if (!isAdmin && order.UserId.HasValue)
        {
            if (!userId.HasValue || order.UserId.Value != userId.Value)
            {
                throw new ForbiddenAccessException("No tienes autorización para cancelar este pedido.");
            }
        }

        // Customer can only cancel PendingPayment orders
        if (!isAdmin && order.Status != OrderStatus.PendingPayment)
        {
            throw new BusinessRuleValidationException("Solo puedes cancelar pedidos que se encuentren pendientes de pago.");
        }

        return await UpdateOrderStatusAsync(
            orderId,
            OrderStatus.Cancelled,
            isAdmin ? "Admin" : "Customer",
            reason ?? "Cancelado por el usuario",
            cancellationToken);
    }

    public async Task<PaymentInitiationResult> InitiateOrderPaymentAsync(
        Guid orderId,
        Guid? userId,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            throw new NotFoundException("Order", orderId);
        }

        if (userId.HasValue && order.UserId.HasValue && order.UserId.Value != userId.Value)
        {
            throw new ForbiddenAccessException("No tienes autorización para realizar el pago de este pedido.");
        }

        if (order.Status != OrderStatus.PendingPayment)
        {
            throw new BusinessRuleValidationException($"El pedido {order.OrderNumber} no se encuentra en estado pendiente de pago (Estado actual: {order.Status}).");
        }

        var result = await _paymentProvider.InitiatePaymentAsync(new PaymentInitiationRequest(
            OrderId: order.Id,
            OrderNumber: order.OrderNumber,
            Amount: order.Total,
            Currency: "USD",
            CustomerEmail: order.User?.Email ?? order.GuestEmail,
            ReturnUrl: returnUrl,
            CancelUrl: cancelUrl
        ), cancellationToken);

        // Record payment attempt
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Provider = _paymentProvider.ProviderName,
            TransactionId = result.TransactionId,
            Amount = order.Total,
            Currency = "USD",
            Status = result.Success ? PaymentAttemptStatus.Initiated : PaymentAttemptStatus.Failed,
            ErrorMessage = result.ErrorMessage
        };

        _context.PaymentAttempts.Add(attempt);
        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<bool> ProcessPaymentWebhookAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default)
    {
        var webhookResult = await _paymentProvider.ProcessWebhookAsync(payload, signatureHeader, cancellationToken);

        if (!webhookResult.IsValidSignature || !webhookResult.Handled)
        {
            _logger.LogWarning("Payment webhook rejected: SignatureValid={Sig}, Handled={Handled}, Error={Err}",
                webhookResult.IsValidSignature, webhookResult.Handled, webhookResult.ErrorMessage);
            return false;
        }

        if (!webhookResult.OrderId.HasValue && string.IsNullOrWhiteSpace(webhookResult.OrderNumber))
        {
            _logger.LogWarning("Webhook did not provide OrderId or OrderNumber");
            return false;
        }

        // Find Order
        var order = await _context.Orders
            .Include(o => o.Reservations)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o =>
                (webhookResult.OrderId.HasValue && o.Id == webhookResult.OrderId.Value) ||
                (!string.IsNullOrWhiteSpace(webhookResult.OrderNumber) && o.OrderNumber == webhookResult.OrderNumber),
                cancellationToken);

        if (order == null)
        {
            _logger.LogError("Webhook received for unknown order: {OrderId} / {OrderNumber}",
                webhookResult.OrderId, webhookResult.OrderNumber);
            return false;
        }

        // Idempotency: check if transaction already registered as succeeded
        var existingSuccessAttempt = await _context.PaymentAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TransactionId == webhookResult.TransactionId && p.Status == PaymentAttemptStatus.Succeeded, cancellationToken);

        if (existingSuccessAttempt != null)
        {
            _logger.LogInformation("Webhook transaction {Txn} was already processed successfully. Skipping duplicate execution.",
                webhookResult.TransactionId);
            return true;
        }

        // Record attempt
        var attempt = new PaymentAttempt
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Provider = _paymentProvider.ProviderName,
            TransactionId = webhookResult.TransactionId ?? $"txn_{Guid.NewGuid():N}",
            Amount = webhookResult.Amount,
            Currency = "USD",
            Status = webhookResult.IsSuccess ? PaymentAttemptStatus.Succeeded : PaymentAttemptStatus.Failed,
            ErrorMessage = webhookResult.ErrorMessage,
            RawPayloadJson = payload
        };
        _context.PaymentAttempts.Add(attempt);

        if (webhookResult.IsSuccess)
        {
            if (order.Status == OrderStatus.PendingPayment)
            {
                await UpdateOrderStatusAsync(
                    order.Id,
                    OrderStatus.Paid,
                    "PaymentWebhook",
                    $"Pago confirmado por pasarela ({_paymentProvider.ProviderName}). Txn: {webhookResult.TransactionId}",
                    cancellationToken);
            }
            else
            {
                _logger.LogInformation("Order {OrderNumber} was already in state {Status}. Recorded payment attempt without re-transitioning.",
                    order.OrderNumber, order.Status);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            _logger.LogWarning("Payment failed for Order {OrderNumber}: {Error}", order.OrderNumber, webhookResult.ErrorMessage);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> CleanupExpiredReservationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var expiredReservations = await _context.StockReservations
            .Include(r => r.Order)
            .Where(r => r.Status == ReservationStatus.Pending && r.ExpiresAtUtc <= now)
            .ToListAsync(cancellationToken);

        if (!expiredReservations.Any())
        {
            return 0;
        }

        var ordersToCancel = expiredReservations
            .Select(r => r.Order)
            .DistinctBy(o => o.Id)
            .Where(o => o.Status == OrderStatus.PendingPayment)
            .ToList();

        int cancelledCount = 0;

        foreach (var order in ordersToCancel)
        {
            try
            {
                await UpdateOrderStatusAsync(
                    order.Id,
                    OrderStatus.Cancelled,
                    "SystemCleanupWorker",
                    "Reserva expirada (tiempo límite de pago de 15 minutos excedido)",
                    cancellationToken);

                cancelledCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel expired order {OrderNumber}", order.OrderNumber);
            }
        }

        return cancelledCount;
    }

    private static void ValidateStatusTransition(OrderStatus current, OrderStatus target)
    {
        // Terminal states cannot transition
        if (current == OrderStatus.Cancelled || current == OrderStatus.Refunded)
        {
            throw new BusinessRuleValidationException(
                $"No se puede cambiar el estado de una orden que ya está en estado terminal '{current}'.");
        }

        var isValid = (current, target) switch
        {
            (OrderStatus.PendingPayment, OrderStatus.Paid) => true,
            (OrderStatus.PendingPayment, OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.Processing) => true,
            (OrderStatus.Paid, OrderStatus.Cancelled) => true,
            (OrderStatus.Paid, OrderStatus.Refunded) => true,
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Processing, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            (OrderStatus.Shipped, OrderStatus.Refunded) => true,
            (OrderStatus.Delivered, OrderStatus.Refunded) => true,
            _ => false
        };

        if (!isValid)
        {
            throw new BusinessRuleValidationException(
                $"Transición de estado inválida: no es posible pasar de '{current}' a '{target}'.");
        }
    }

    private static OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto(
            Id: order.Id,
            OrderNumber: order.OrderNumber,
            PublicId: order.PublicId,
            UserId: order.UserId,
            GuestEmail: order.GuestEmail,
            Status: order.Status.ToString(),
            Subtotal: order.Subtotal,
            DiscountTotal: order.DiscountTotal,
            ShippingCost: order.ShippingCost,
            Total: order.Total,
            CouponCode: order.CouponCode,
            ShippingMethod: order.ShippingMethod,
            ShippingAddressSnapshotJson: order.ShippingAddressSnapshotJson,
            CustomerNotes: order.CustomerNotes,
            PaidAtUtc: order.PaidAtUtc,
            ShippedAtUtc: order.ShippedAtUtc,
            DeliveredAtUtc: order.DeliveredAtUtc,
            CancelledAtUtc: order.CancelledAtUtc,
            CancellationReason: order.CancellationReason,
            CreatedAtUtc: order.CreatedAtUtc,
            Items: order.Items.Select(i => new OrderItemDto(
                Id: i.Id,
                ProductVariantId: i.ProductVariantId,
                SkuSnapshot: i.SkuSnapshot,
                ProductNameSnapshot: i.ProductNameSnapshot,
                UnitPrice: i.UnitPrice,
                Quantity: i.Quantity,
                TotalPrice: i.TotalPrice,
                AttributesJsonSnapshot: i.AttributesJsonSnapshot,
                ImageUrlSnapshot: i.ImageUrlSnapshot
            )).ToList(),
            StatusHistory: order.StatusHistory.Select(h => new OrderStatusHistoryDto(
                PreviousStatus: h.PreviousStatus.ToString(),
                NewStatus: h.NewStatus.ToString(),
                ChangedBy: h.ChangedBy,
                Reason: h.Reason,
                CreatedAtUtc: h.CreatedAtUtc
            )).ToList()
        );
    }

    private static string GenerateOrderNumber()
    {
        var yearMonth = DateTime.UtcNow.ToString("yyyyMM");
        var randomPart = Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToUpperInvariant();
        return $"HS-{yearMonth}-{randomPart}";
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
