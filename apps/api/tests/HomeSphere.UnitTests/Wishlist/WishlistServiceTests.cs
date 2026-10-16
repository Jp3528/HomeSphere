using HomeSphere.Application.Features.Wishlist;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using HomeSphere.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.UnitTests.Wishlist;

public class WishlistServiceTests
{
    private readonly HomeSphereDbContext _dbContext;
    private readonly WishlistService _wishlistService;

    public WishlistServiceTests()
    {
        var options = new DbContextOptionsBuilder<HomeSphereDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HomeSphereDbContext(options);
        _wishlistService = new WishlistService(_dbContext);
    }

    private Product CreateTestProduct(string name, ProductStatus status = ProductStatus.Active, decimal price = 100m)
    {
        var category = new Category { Name = "Salas", Slug = "salas" };
        var brand = new Brand { Name = "Nordic", Slug = "nordic" };

        var product = new Product
        {
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
            BasePrice = price,
            Status = status,
            Category = category,
            Brand = brand,
            Variants = new List<ProductVariant>
            {
                new()
                {
                    Sku = $"SKU-{Guid.NewGuid():N}"[..10],
                    Price = price,
                    StockQuantity = 10,
                    IsActive = true
                }
            }
        };

        _dbContext.Products.Add(product);
        return product;
    }

    [Fact]
    public async Task AddToWishlist_WhenValidProduct_ShouldCreateItem()
    {
        var user = new User { Email = "user@test.com" };
        var product = CreateTestProduct("Sillón Velvet");
        await _dbContext.SaveChangesAsync();

        var item = await _wishlistService.AddToWishlistAsync(user.Id, product.Id);

        Assert.NotNull(item);
        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal("Sillón Velvet", item.ProductName);
        Assert.True(item.IsAvailable);

        var inDb = await _dbContext.WishlistItems.FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == product.Id);
        Assert.NotNull(inDb);
    }

    [Fact]
    public async Task AddToWishlist_WhenProductDoesNotExist_ShouldThrowNotFoundException()
    {
        var user = new User { Email = "user@test.com" };
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _wishlistService.AddToWishlistAsync(user.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task AddToWishlist_WhenProductArchived_ShouldThrowBusinessRuleValidationException()
    {
        var user = new User { Email = "user@test.com" };
        var product = CreateTestProduct("Mesa Antigua", ProductStatus.Archived);
        await _dbContext.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            _wishlistService.AddToWishlistAsync(user.Id, product.Id));

        Assert.Contains("archivado", ex.Message);
    }

    [Fact]
    public async Task AddToWishlist_WhenAlreadyInWishlist_ShouldReturnExistingItemIdempotently()
    {
        var user = new User { Email = "user@test.com" };
        var product = CreateTestProduct("Lámpara Glow");
        await _dbContext.SaveChangesAsync();

        var first = await _wishlistService.AddToWishlistAsync(user.Id, product.Id);
        var second = await _wishlistService.AddToWishlistAsync(user.Id, product.Id);

        Assert.Equal(first.Id, second.Id);
        var count = await _dbContext.WishlistItems.CountAsync(w => w.UserId == user.Id && w.ProductId == product.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RemoveFromWishlist_WhenExists_ShouldReturnTrueAndRemove()
    {
        var user = new User { Email = "user@test.com" };
        var product = CreateTestProduct("Espejo Oval");
        await _dbContext.SaveChangesAsync();

        await _wishlistService.AddToWishlistAsync(user.Id, product.Id);
        var removed = await _wishlistService.RemoveFromWishlistAsync(user.Id, product.Id);

        Assert.True(removed);
        var inDb = await _dbContext.WishlistItems.AnyAsync(w => w.UserId == user.Id && w.ProductId == product.Id);
        Assert.False(inDb);
    }

    [Fact]
    public async Task RemoveFromWishlist_WhenDoesNotExist_ShouldReturnFalse()
    {
        var removed = await _wishlistService.RemoveFromWishlistAsync(Guid.NewGuid(), Guid.NewGuid());
        Assert.False(removed);
    }

    [Fact]
    public async Task IsInWishlist_ShouldReturnCorrectStatus()
    {
        var user = new User { Email = "user@test.com" };
        var product = CreateTestProduct("Alfombra Lana");
        await _dbContext.SaveChangesAsync();

        Assert.False(await _wishlistService.IsInWishlistAsync(user.Id, product.Id));

        await _wishlistService.AddToWishlistAsync(user.Id, product.Id);

        Assert.True(await _wishlistService.IsInWishlistAsync(user.Id, product.Id));
    }

    [Fact]
    public async Task GetUserWishlist_ShouldReturnOnlyCurrentUserItems_AntiIdor()
    {
        var user1 = new User { Email = "u1@test.com" };
        var user2 = new User { Email = "u2@test.com" };
        var p1 = CreateTestProduct("Producto 1");
        var p2 = CreateTestProduct("Producto 2");
        await _dbContext.SaveChangesAsync();

        await _wishlistService.AddToWishlistAsync(user1.Id, p1.Id);
        await _wishlistService.AddToWishlistAsync(user2.Id, p2.Id);

        var u1Wishlist = await _wishlistService.GetUserWishlistAsync(user1.Id);

        Assert.Single(u1Wishlist);
        Assert.Equal(p1.Id, u1Wishlist[0].ProductId);
    }
}
