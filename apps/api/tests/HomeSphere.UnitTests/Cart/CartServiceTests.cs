using HomeSphere.Application.Features.Cart;
using HomeSphere.Application.Features.Cart.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using HomeSphere.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HomeSphere.UnitTests.Cart;

public class CartServiceTests
{
    private readonly HomeSphereDbContext _dbContext;
    private readonly CartService _cartService;

    public CartServiceTests()
    {
        var options = new DbContextOptionsBuilder<HomeSphereDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HomeSphereDbContext(options);
        _cartService = new CartService(_dbContext);
    }

    private async Task<(Category category, Brand brand, User user)> SeedPrerequisitesAsync()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Muebles",
            Slug = "muebles-" + Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow
        };
        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            Name = "Nordic Design",
            Slug = "nordic-" + Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "shopper@test.com",
            FirstName = "Cliente",
            LastName = "Test",
            PasswordHash = "hashed_pw",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Categories.Add(category);
        _dbContext.Brands.Add(brand);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return (category, brand, user);
    }

    [Fact]
    public async Task AddItem_WhenValidProductVariant_ShouldAddToCartAndRecalculateSubtotal()
    {
        // Arrange
        var (category, brand, _) = await SeedPrerequisitesAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Sillón Nórdico",
            Slug = "sillon-nordico",
            ShortDescription = "Sillón elegante",
            Description = "Descripción completa",
            BasePrice = 350.00m,
            Status = ProductStatus.Active,
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "SIL-NOR-01",
            Name = "Gris Ceniza",
            Price = 350.00m,
            StockQuantity = 10,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(variant);
        await _dbContext.SaveChangesAsync();

        var guestToken = "test_guest_token_1";

        // Act
        var result = await _cartService.AddItemAsync(null, guestToken, new AddToCartRequest(variant.Id, 2));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(700.00m, result.Subtotal);
        Assert.Single(result.Items);
        Assert.Equal(variant.Id, result.Items[0].ProductVariantId);
        Assert.Equal("Sillón Nórdico", result.Items[0].ProductName);
        Assert.True(result.Items[0].IsAvailable);
        Assert.Null(result.Items[0].Warning);
    }

    [Fact]
    public async Task AddItem_WhenOutOfStock_ShouldThrowBusinessRuleValidationException()
    {
        // Arrange
        var (category, brand, _) = await SeedPrerequisitesAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Mesa Vintage",
            Slug = "mesa-vintage",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 500m,
            Status = ProductStatus.Active,
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "MES-VIN-01",
            Name = "Nogal",
            Price = 500.00m,
            StockQuantity = 0, // Out of stock
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(variant);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            _cartService.AddItemAsync(null, "token_out_of_stock", new AddToCartRequest(variant.Id, 1)));
    }

    [Fact]
    public async Task AddItem_WhenItemAlreadyInCart_ShouldAccumulateQuantity()
    {
        // Arrange
        var (category, brand, user) = await SeedPrerequisitesAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Lámpara Minimalista",
            Slug = "lampara-min",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 80m,
            Status = ProductStatus.Active,
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "LAM-MIN-01",
            Name = "Latón",
            Price = 80.00m,
            StockQuantity = 20,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(variant);
        await _dbContext.SaveChangesAsync();

        // Act
        await _cartService.AddItemAsync(user.Id, null, new AddToCartRequest(variant.Id, 2));
        var updated = await _cartService.AddItemAsync(user.Id, null, new AddToCartRequest(variant.Id, 3));

        // Assert
        Assert.Single(updated.Items);
        Assert.Equal(5, updated.TotalItems);
        Assert.Equal(400.00m, updated.Subtotal);
    }

    [Fact]
    public async Task UpdateItemQuantity_WhenQuantityIsZero_ShouldRemoveItem()
    {
        // Arrange
        var (category, brand, _) = await SeedPrerequisitesAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Espejo Sol",
            Slug = "espejo-sol",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 120m,
            Status = ProductStatus.Active,
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "ESP-SOL-01",
            Name = "Dorado",
            Price = 120.00m,
            StockQuantity = 15,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(variant);
        await _dbContext.SaveChangesAsync();

        var guestToken = "guest_for_delete";
        var cart = await _cartService.AddItemAsync(null, guestToken, new AddToCartRequest(variant.Id, 2));
        var itemId = cart.Items[0].Id;

        // Act
        var afterZero = await _cartService.UpdateItemQuantityAsync(null, guestToken, itemId, 0);

        // Assert
        Assert.Empty(afterZero.Items);
        Assert.Equal(0, afterZero.TotalItems);
        Assert.Equal(0m, afterZero.Subtotal);
    }

    [Fact]
    public async Task MergeCart_WhenGuestCartHasItemsAndUserCartExists_ShouldMergeDeterministically()
    {
        // Arrange
        var (category, brand, user) = await SeedPrerequisitesAsync();

        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Cojín Terciopelo",
            Slug = "cojin-terciopelo",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 30m,
            Status = ProductStatus.Active,
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        var v1 = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product1.Id,
            Sku = "COJ-TER-01",
            Name = "Azul Noche",
            Price = 30.00m,
            StockQuantity = 50,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Manta Lana",
            Slug = "manta-lana",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 75m,
            Status = ProductStatus.Active,
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        var v2 = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product2.Id,
            Sku = "MAN-LAN-01",
            Name = "Beige",
            Price = 75.00m,
            StockQuantity = 30,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.AddRange(product1, product2);
        _dbContext.ProductVariants.AddRange(v1, v2);
        await _dbContext.SaveChangesAsync();

        var guestToken = "guest_to_merge_123";

        // User already has 1x v1 in cart
        await _cartService.AddItemAsync(user.Id, null, new AddToCartRequest(v1.Id, 1));

        // Guest has 2x v1 and 1x v2 in cart
        await _cartService.AddItemAsync(null, guestToken, new AddToCartRequest(v1.Id, 2));
        await _cartService.AddItemAsync(null, guestToken, new AddToCartRequest(v2.Id, 1));

        // Act: User logs in, triggers merge
        var merged = await _cartService.MergeCartAsync(user.Id, guestToken);

        // Assert
        Assert.NotNull(merged);
        Assert.Equal(2, merged.Items.Count);
        // v1 should have 1 + 2 = 3 items
        var itemV1 = merged.Items.First(i => i.ProductVariantId == v1.Id);
        Assert.Equal(3, itemV1.Quantity);
        Assert.Equal(90.00m, itemV1.LineTotal);

        // v2 should have 1 item
        var itemV2 = merged.Items.First(i => i.ProductVariantId == v2.Id);
        Assert.Equal(1, itemV2.Quantity);
        Assert.Equal(75.00m, itemV2.LineTotal);

        Assert.Equal(4, merged.TotalItems);
        Assert.Equal(165.00m, merged.Subtotal);

        // Guest cart entity should no longer exist in DB
        var oldGuestCart = await _dbContext.Carts.FirstOrDefaultAsync(c => c.GuestToken == guestToken);
        Assert.Null(oldGuestCart);
    }

    [Fact]
    public async Task MergeCart_WhenUserHasNoExistingCart_ShouldReassignGuestCartDirectly()
    {
        // Arrange
        var (category, brand, user) = await SeedPrerequisitesAsync();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Florero Cerámica",
            Slug = "florero-ceramica",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 45m,
            Status = ProductStatus.Active,
            CategoryId = category.Id,
            BrandId = brand.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "FLO-CER-01",
            Name = "Terracota",
            Price = 45.00m,
            StockQuantity = 20,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(variant);
        await _dbContext.SaveChangesAsync();

        var guestToken = "guest_reassign_test";

        // Guest has 2x in cart
        await _cartService.AddItemAsync(null, guestToken, new AddToCartRequest(variant.Id, 2));

        // Act: User logs in (has no prior cart)
        var merged = await _cartService.MergeCartAsync(user.Id, guestToken);

        // Assert
        Assert.NotNull(merged);
        Assert.Equal(user.Id, merged.UserId);
        Assert.Null(merged.GuestToken);
        Assert.Equal(2, merged.TotalItems);
        Assert.Equal(90.00m, merged.Subtotal);
    }
}
