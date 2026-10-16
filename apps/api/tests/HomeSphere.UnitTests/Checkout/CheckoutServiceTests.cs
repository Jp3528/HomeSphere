using HomeSphere.Application.Features.Cart;
using HomeSphere.Application.Features.Cart.DTOs;
using HomeSphere.Application.Features.Checkout;
using HomeSphere.Application.Features.Checkout.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using HomeSphere.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HomeSphere.UnitTests.Checkout;

public class CheckoutServiceTests
{
    private readonly HomeSphereDbContext _dbContext;
    private readonly CartService _cartService;
    private readonly CheckoutService _checkoutService;

    public CheckoutServiceTests()
    {
        var options = new DbContextOptionsBuilder<HomeSphereDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HomeSphereDbContext(options);
        _cartService = new CartService(_dbContext);
        _checkoutService = new CheckoutService(_dbContext);
    }

    private async Task<(Category category, Brand brand, User user)> SeedPrerequisitesAsync()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Comedor",
            Slug = "comedor-" + Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow
        };
        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            Name = "Stockholm Wood",
            Slug = "stockholm-" + Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "buyer@test.com",
            FirstName = "Cliente",
            LastName = "Comprador",
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Categories.Add(category);
        _dbContext.Brands.Add(brand);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return (category, brand, user);
    }

    [Fact]
    public async Task GetCheckoutPreview_WhenCartHasItems_ShouldCalculateCorrectSubtotalAndStandardShipping()
    {
        // Arrange
        var (category, brand, _) = await SeedPrerequisitesAsync();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Silla Comedor",
            Slug = "silla-comedor",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 150m,
            Status = ProductStatus.Active,
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "SIL-COM-01",
            Name = "Roble",
            Price = 150m,
            StockQuantity = 10,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(variant);
        await _dbContext.SaveChangesAsync();

        var guestToken = "checkout_guest_1";
        await _cartService.AddItemAsync(null, guestToken, new AddToCartRequest(variant.Id, 2)); // 2 * $150 = $300

        // Act
        var preview = await _checkoutService.GetCheckoutPreviewAsync(null, guestToken, new CheckoutPreviewRequest());

        // Assert
        Assert.NotNull(preview);
        Assert.Equal(300m, preview.Subtotal);
        Assert.Equal(15m, preview.Shipping.Cost); // Standard shipping < $1000 is $15
        Assert.False(preview.Shipping.IsFree);
        Assert.Equal(0m, preview.DiscountTotal);
        Assert.Equal(315m, preview.Total);
        Assert.True(preview.CanProceedToOrder);
        Assert.Empty(preview.ValidationErrors);
    }

    [Fact]
    public async Task GetCheckoutPreview_WhenSubtotalAbove1000_StandardShippingShouldBeFree()
    {
        // Arrange
        var (category, brand, _) = await SeedPrerequisitesAsync();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Mesa Grande",
            Slug = "mesa-grande",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 1200m,
            Status = ProductStatus.Active,
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "MES-GRA-01",
            Name = "Nogal Macizo",
            Price = 1200m,
            StockQuantity = 5,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(variant);
        await _dbContext.SaveChangesAsync();

        var guestToken = "free_shipping_guest";
        await _cartService.AddItemAsync(null, guestToken, new AddToCartRequest(variant.Id, 1)); // $1200 >= $1000

        // Act
        var preview = await _checkoutService.GetCheckoutPreviewAsync(null, guestToken, new CheckoutPreviewRequest());

        // Assert
        Assert.Equal(1200m, preview.Subtotal);
        Assert.Equal(0m, preview.Shipping.Cost);
        Assert.True(preview.Shipping.IsFree);
        Assert.Equal(1200m, preview.Total);
    }

    [Fact]
    public async Task GetCheckoutPreview_WhenPercentageCouponApplied_ShouldDeductDiscountUpToMaxLimit()
    {
        // Arrange
        var (category, brand, _) = await SeedPrerequisitesAsync();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Sofá Modular",
            Slug = "sofa-modular",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 2000m,
            Status = ProductStatus.Active,
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "SOF-MOD-01",
            Name = "Gris Perla",
            Price = 2000m,
            StockQuantity = 5,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var coupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = "DESCUENTO15",
            Description = "15% con tope de $150",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 15m,
            MaxDiscountAmount = 150m, // 15% of $2000 is $300, capped at $150
            ValidFromUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ValidUntilUtc = DateTimeOffset.UtcNow.AddDays(10),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(variant);
        _dbContext.Coupons.Add(coupon);
        await _dbContext.SaveChangesAsync();

        var guestToken = "coupon_test_guest";
        await _cartService.AddItemAsync(null, guestToken, new AddToCartRequest(variant.Id, 1));

        // Act
        var preview = await _checkoutService.GetCheckoutPreviewAsync(null, guestToken, new CheckoutPreviewRequest(CouponCode: "DESCUENTO15"));

        // Assert
        Assert.NotNull(preview.AppliedCoupon);
        Assert.Equal("DESCUENTO15", preview.AppliedCoupon.Code);
        Assert.Equal(150m, preview.DiscountTotal); // Capped at $150
        Assert.Equal(1850m, preview.Total); // $2000 - $150 + $0 shipping
    }

    [Fact]
    public async Task GetCheckoutPreview_WhenCouponExpired_ShouldReportValidationError()
    {
        // Arrange
        var (category, brand, _) = await SeedPrerequisitesAsync();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Lamparita",
            Slug = "lamparita",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 100m,
            Status = ProductStatus.Active,
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "LAM-01",
            Name = "Blanco",
            Price = 100m,
            StockQuantity = 10,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var expiredCoupon = new Coupon
        {
            Id = Guid.NewGuid(),
            Code = "EXPIRED50",
            Description = "50% vencido",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 50m,
            ValidFromUtc = DateTimeOffset.UtcNow.AddDays(-30),
            ValidUntilUtc = DateTimeOffset.UtcNow.AddDays(-1), // Already expired
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(variant);
        _dbContext.Coupons.Add(expiredCoupon);
        await _dbContext.SaveChangesAsync();

        var guestToken = "expired_coupon_guest";
        await _cartService.AddItemAsync(null, guestToken, new AddToCartRequest(variant.Id, 1));

        // Act
        var preview = await _checkoutService.GetCheckoutPreviewAsync(null, guestToken, new CheckoutPreviewRequest(CouponCode: "EXPIRED50"));

        // Assert
        Assert.Null(preview.AppliedCoupon);
        Assert.Equal(0m, preview.DiscountTotal);
        Assert.Contains(preview.ValidationErrors, e => e.Contains("expiró"));
    }

    [Fact]
    public async Task GetCheckoutPreview_WhenAddressBelongsToAnotherUser_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var (_, _, user1) = await SeedPrerequisitesAsync();
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "intruder@test.com",
            FirstName = "Intruder",
            LastName = "User",
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow
        };
        var user2Address = new Address
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            RecipientName = "Intruder User",
            PhoneNumber = "+51999888777",
            Line1 = "Calle Prohibida 123",
            District = "Miraflores",
            City = "Lima",
            Region = "Lima",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Users.Add(user2);
        _dbContext.Addresses.Add(user2Address);
        await _dbContext.SaveChangesAsync();

        // User1 attempts to preview checkout using User2's address ID
        var request = new CheckoutPreviewRequest(AddressId: user2Address.Id);

        // Act & Assert (Anti-IDOR protection)
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _checkoutService.GetCheckoutPreviewAsync(user1.Id, null, request));
    }
}
