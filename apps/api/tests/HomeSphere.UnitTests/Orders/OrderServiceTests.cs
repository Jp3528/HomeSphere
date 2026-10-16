using System.Text.Json;
using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Addresses.DTOs;
using HomeSphere.Application.Features.Orders;
using HomeSphere.Application.Features.Orders.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using HomeSphere.Infrastructure.Persistence;
using HomeSphere.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using CartEntity = HomeSphere.Domain.Entities.Cart;

namespace HomeSphere.UnitTests.Orders;

public class OrderServiceTests
{
    private readonly HomeSphereDbContext _dbContext;
    private readonly MockPaymentProvider _paymentProvider;
    private readonly OrderService _orderService;
    private readonly string _webhookSecret = "test-webhook-secret-key-32chars-long!!";

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<HomeSphereDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HomeSphereDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payment:WebhookSecret"] = _webhookSecret
            })
            .Build();

        _paymentProvider = new MockPaymentProvider(config, NullLogger<MockPaymentProvider>.Instance);
        _orderService = new OrderService(_dbContext, _paymentProvider, NullLogger<OrderService>.Instance);
    }

    private async Task<(User user, ProductVariant variant, Address address)> SeedProductAndUserAsync(int initialStock = 10, decimal price = 250m)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "buyer@homesphere.com",
            FirstName = "Sofia",
            LastName = "Alvarez",
            PasswordHash = "hashed",
            CreatedAtUtc = DateTime.UtcNow
        };

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Sala",
            Slug = "sala-" + Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow
        };

        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            Name = "Koto Design",
            Slug = "koto-" + Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Sillón Nórdico Oslo",
            Slug = "sillon-oslo-" + Guid.NewGuid().ToString("N"),
            ShortDescription = "Diseño ergonómico nórdico",
            Description = "Tapizado de lino y roble",
            BasePrice = price,
            CategoryId = category.Id,
            BrandId = brand.Id,
            Status = ProductStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            Name = "Gris Claro / Roble",
            Sku = "OSLO-GRIS-" + Guid.NewGuid().ToString("N")[..6].ToUpper(),
            Price = price,
            StockQuantity = initialStock,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var address = new Address
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Label = "Casa",
            RecipientName = "Sofia Alvarez",
            PhoneNumber = "+51999888777",
            Line1 = "Av. Las Palmeras 450",
            District = "Miraflores",
            City = "Lima",
            Region = "Lima",
            IsDefault = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Users.Add(user);
        _dbContext.Categories.Add(category);
        _dbContext.Brands.Add(brand);
        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(variant);
        _dbContext.Addresses.Add(address);
        await _dbContext.SaveChangesAsync();

        return (user, variant, address);
    }

    private async Task<CartEntity> SeedCartAsync(Guid userId, Guid variantId, int quantity)
    {
        var cart = new CartEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync();

        var cartItem = new CartItem
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            ProductVariantId = variantId,
            Quantity = quantity,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.CartItems.Add(cartItem);
        await _dbContext.SaveChangesAsync();

        return cart;
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidStock_CreatesOrderSnapshotsAndDeductsStock()
    {
        // Arrange
        var (user, variant, address) = await SeedProductAndUserAsync(initialStock: 10, price: 300m);
        await SeedCartAsync(user.Id, variant.Id, quantity: 2);

        var request = new CreateOrderRequest(
            ShippingAddressId: address.Id,
            GuestShippingAddress: null,
            ShippingMethod: "standard",
            CouponCode: null
        );

        // Act
        var order = await _orderService.CreateOrderAsync(request, user.Id, null, null);

        // Assert
        Assert.NotNull(order);
        Assert.Equal("PendingPayment", order.Status);
        Assert.Equal(600m, order.Subtotal);
        Assert.Equal(15m, order.ShippingCost);
        Assert.Equal(615m, order.Total);
        Assert.Single(order.Items);
        Assert.Equal("OSLO-GRIS-", order.Items[0].SkuSnapshot[..10]);
        Assert.Equal(2, order.Items[0].Quantity);
        Assert.Equal(300m, order.Items[0].UnitPrice);

        // Verify stock deducted in DB
        var updatedVariant = await _dbContext.ProductVariants.FindAsync(variant.Id);
        Assert.NotNull(updatedVariant);
        Assert.Equal(8, updatedVariant.StockQuantity);

        // Verify reservation created
        var reservation = await _dbContext.StockReservations.FirstOrDefaultAsync(r => r.OrderId == order.Id);
        Assert.NotNull(reservation);
        Assert.Equal(ReservationStatus.Pending, reservation.Status);
        Assert.Equal(2, reservation.Quantity);

        // Verify cart cleared
        var cart = await _dbContext.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == user.Id);
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task CreateOrderAsync_WithInsufficientStock_ThrowsAndDoesNotDeductStock()
    {
        // Arrange
        var (user, variant, address) = await SeedProductAndUserAsync(initialStock: 1, price: 300m);
        await SeedCartAsync(user.Id, variant.Id, quantity: 3);

        var request = new CreateOrderRequest(
            ShippingAddressId: address.Id,
            GuestShippingAddress: null,
            ShippingMethod: "standard",
            CouponCode: null
        );

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            _orderService.CreateOrderAsync(request, user.Id, null, null));

        // Stock must remain unchanged
        var updatedVariant = await _dbContext.ProductVariants.FindAsync(variant.Id);
        Assert.NotNull(updatedVariant);
        Assert.Equal(1, updatedVariant.StockQuantity);
        Assert.Empty(await _dbContext.Orders.ToListAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_WithSameIdempotencyKey_ReturnsCachedOrderWithoutDuplicate()
    {
        // Arrange
        var (user, variant, address) = await SeedProductAndUserAsync(initialStock: 10, price: 500m);
        await SeedCartAsync(user.Id, variant.Id, quantity: 1);

        var idempotencyKey = "idemp-key-" + Guid.NewGuid().ToString("N");
        var request = new CreateOrderRequest(
            ShippingAddressId: address.Id,
            GuestShippingAddress: null,
            ShippingMethod: "standard",
            CouponCode: null
        );

        // Act 1: Initial creation
        var order1 = await _orderService.CreateOrderAsync(request, user.Id, null, idempotencyKey);
        Assert.NotNull(order1);

        // Act 2: Retry with exact same Idempotency-Key
        var order2 = await _orderService.CreateOrderAsync(request, user.Id, null, idempotencyKey);
        Assert.NotNull(order2);

        // Assert: Same public ID, stock deducted only once (10 - 1 = 9)
        Assert.Equal(order1.PublicId, order2.PublicId);
        Assert.Equal(order1.OrderNumber, order2.OrderNumber);

        var updatedVariant = await _dbContext.ProductVariants.FindAsync(variant.Id);
        Assert.NotNull(updatedVariant);
        Assert.Equal(9, updatedVariant.StockQuantity);

        // Exactly one order in DB
        Assert.Single(await _dbContext.Orders.ToListAsync());
    }

    [Fact]
    public async Task CreateOrderAsync_WithAddressBelongingToAnotherUser_ThrowsForbiddenAccessException()
    {
        // Arrange
        var (userA, variant, addressA) = await SeedProductAndUserAsync(initialStock: 10, price: 100m);
        var userB = new User
        {
            Id = Guid.NewGuid(),
            Email = "userB@homesphere.com",
            FirstName = "Impostor",
            LastName = "User",
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Users.Add(userB);
        await _dbContext.SaveChangesAsync();
        await SeedCartAsync(userB.Id, variant.Id, quantity: 1);

        // User B attempts to checkout using Address of User A
        var request = new CreateOrderRequest(
            ShippingAddressId: addressA.Id,
            GuestShippingAddress: null,
            ShippingMethod: "standard",
            CouponCode: null
        );

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _orderService.CreateOrderAsync(request, userB.Id, null, null));
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidCoupon_AppliesDiscountAndIncrementsTimesUsed()
    {
        // Arrange
        var (user, variant, address) = await SeedProductAndUserAsync(initialStock: 10, price: 1200m);
        await SeedCartAsync(user.Id, variant.Id, quantity: 1);

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = "DESCUENTO10",
            Description = "10% off",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m,
            MaxDiscountAmount = 100m,
            ValidFromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ValidUntilUtc = DateTimeOffset.UtcNow.AddDays(7),
            UsageLimit = 10,
            TimesUsed = 0,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Coupons.Add(coupon);
        await _dbContext.SaveChangesAsync();

        var request = new CreateOrderRequest(
            ShippingAddressId: address.Id,
            GuestShippingAddress: null,
            ShippingMethod: "standard",
            CouponCode: "DESCUENTO10"
        );

        // Act
        var order = await _orderService.CreateOrderAsync(request, user.Id, null, null);

        // Assert: 10% of 1200 is 120, capped at max discount of 100
        Assert.Equal(1200m, order.Subtotal);
        Assert.Equal(100m, order.DiscountTotal);
        // Subtotal >= 1000 => free standard shipping (0)
        Assert.Equal(0m, order.ShippingCost);
        Assert.Equal(1100m, order.Total);

        var updatedCoupon = await _dbContext.Coupons.FindAsync(coupon.Id);
        Assert.NotNull(updatedCoupon);
        Assert.Equal(1, updatedCoupon.TimesUsed);
    }

    [Fact]
    public async Task PaymentWebhook_WithValidHmac_TransitionsToPaidAndConfirmsReservations()
    {
        // Arrange
        var (user, variant, address) = await SeedProductAndUserAsync(initialStock: 10, price: 400m);
        await SeedCartAsync(user.Id, variant.Id, quantity: 1);

        var order = await _orderService.CreateOrderAsync(new CreateOrderRequest(
            ShippingAddressId: address.Id,
            GuestShippingAddress: null
        ), user.Id, null, null);

        var payload = JsonSerializer.Serialize(new
        {
            eventType = "payment.succeeded",
            transactionId = "txn_mock_998877",
            orderId = order.Id.ToString(),
            orderNumber = order.OrderNumber,
            amount = order.Total,
            status = "succeeded"
        });

        var signature = MockPaymentProvider.ComputeHmacSignature(payload, _webhookSecret);

        // Act
        var processed = await _orderService.ProcessPaymentWebhookAsync(payload, signature);

        // Assert
        Assert.True(processed);

        var updatedOrder = await _orderService.GetOrderByIdAsync(order.Id, user.Id, isAdmin: true);
        Assert.Equal("Paid", updatedOrder.Status);
        Assert.NotNull(updatedOrder.PaidAtUtc);

        var reservation = await _dbContext.StockReservations.FirstOrDefaultAsync(r => r.OrderId == order.Id);
        Assert.NotNull(reservation);
        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);

        var attempt = await _dbContext.PaymentAttempts.FirstOrDefaultAsync(p => p.OrderId == order.Id);
        Assert.NotNull(attempt);
        Assert.Equal(PaymentAttemptStatus.Succeeded, attempt.Status);
    }

    [Fact]
    public async Task PaymentWebhook_WithInvalidSignature_RejectsWebhook()
    {
        // Arrange
        var payload = "{\"eventType\":\"payment.succeeded\"}";
        var invalidSignature = "deadbeefbadf00d";

        // Act
        var processed = await _orderService.ProcessPaymentWebhookAsync(payload, invalidSignature);

        // Assert
        Assert.False(processed);
    }

    [Fact]
    public async Task PaymentWebhook_WhenDuplicate_IsIdempotentAndDoesNotRepeat()
    {
        // Arrange
        var (user, variant, address) = await SeedProductAndUserAsync(initialStock: 10, price: 400m);
        await SeedCartAsync(user.Id, variant.Id, quantity: 1);

        var order = await _orderService.CreateOrderAsync(new CreateOrderRequest(
            ShippingAddressId: address.Id,
            GuestShippingAddress: null
        ), user.Id, null, null);

        var payload = JsonSerializer.Serialize(new
        {
            eventType = "payment.succeeded",
            transactionId = "txn_mock_dup_123",
            orderId = order.Id.ToString(),
            orderNumber = order.OrderNumber,
            amount = order.Total,
            status = "succeeded"
        });

        var signature = MockPaymentProvider.ComputeHmacSignature(payload, _webhookSecret);

        // First call
        var firstCall = await _orderService.ProcessPaymentWebhookAsync(payload, signature);
        Assert.True(firstCall);

        // Second call with same transaction ID
        var secondCall = await _orderService.ProcessPaymentWebhookAsync(payload, signature);
        Assert.True(secondCall);

        // Assert: Only 1 success attempt recorded, status remains Paid
        var successAttempts = await _dbContext.PaymentAttempts
            .Where(p => p.OrderId == order.Id && p.Status == PaymentAttemptStatus.Succeeded)
            .ToListAsync();
        Assert.Single(successAttempts);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_InvalidTransition_ThrowsBusinessRuleValidationException()
    {
        // Arrange
        var (user, variant, address) = await SeedProductAndUserAsync();
        await SeedCartAsync(user.Id, variant.Id, 1);
        var order = await _orderService.CreateOrderAsync(new CreateOrderRequest(ShippingAddressId: address.Id, GuestShippingAddress: null), user.Id, null, null);

        // Attempting PendingPayment -> Shipped directly without payment
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Shipped, "Admin"));
    }

    [Fact]
    public async Task CancelOrderAsync_WhenPending_RestoresStockAndReleasesReservations()
    {
        // Arrange
        var (user, variant, address) = await SeedProductAndUserAsync(initialStock: 5, price: 100m);
        await SeedCartAsync(user.Id, variant.Id, quantity: 2);
        var order = await _orderService.CreateOrderAsync(new CreateOrderRequest(ShippingAddressId: address.Id, GuestShippingAddress: null), user.Id, null, null);

        // Stock was 5, 2 reserved => 3 in DB
        var variantAfterOrder = await _dbContext.ProductVariants.FindAsync(variant.Id);
        Assert.Equal(3, variantAfterOrder!.StockQuantity);

        // Act: User cancels pending order
        var cancelledOrder = await _orderService.CancelOrderAsync(order.Id, user.Id, isAdmin: false, reason: "Cambio de opinión");

        // Assert
        Assert.Equal("Cancelled", cancelledOrder.Status);
        Assert.NotNull(cancelledOrder.CancelledAtUtc);

        // Stock must be restored to 5
        var restoredVariant = await _dbContext.ProductVariants.FindAsync(variant.Id);
        Assert.Equal(5, restoredVariant!.StockQuantity);

        // Reservation marked as Released
        var reservation = await _dbContext.StockReservations.FirstOrDefaultAsync(r => r.OrderId == order.Id);
        Assert.NotNull(reservation);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
    }

    [Fact]
    public async Task CleanupExpiredReservationsAsync_CancelsExpiredOrdersAndRestoresStock()
    {
        // Arrange
        var (user, variant, address) = await SeedProductAndUserAsync(initialStock: 10, price: 150m);
        await SeedCartAsync(user.Id, variant.Id, quantity: 3);
        var order = await _orderService.CreateOrderAsync(new CreateOrderRequest(ShippingAddressId: address.Id, GuestShippingAddress: null), user.Id, null, null);

        // Force reservation expiry to 20 minutes ago
        var reservation = await _dbContext.StockReservations.FirstOrDefaultAsync(r => r.OrderId == order.Id);
        Assert.NotNull(reservation);
        reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-20);
        await _dbContext.SaveChangesAsync();

        // Act
        var cleanedCount = await _orderService.CleanupExpiredReservationsAsync();

        // Assert
        Assert.Equal(1, cleanedCount);

        var updatedOrder = await _dbContext.Orders.FindAsync(order.Id);
        Assert.NotNull(updatedOrder);
        Assert.Equal(OrderStatus.Cancelled, updatedOrder.Status);

        // Stock restored (10 - 3 + 3 = 10)
        var restoredVariant = await _dbContext.ProductVariants.FindAsync(variant.Id);
        Assert.Equal(10, restoredVariant!.StockQuantity);
    }
}
