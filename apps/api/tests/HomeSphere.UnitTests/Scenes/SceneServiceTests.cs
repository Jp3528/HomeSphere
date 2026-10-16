using HomeSphere.Application.Features.Scenes;
using HomeSphere.Application.Features.Scenes.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using HomeSphere.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.UnitTests.Scenes;

public class SceneServiceTests
{
    private readonly HomeSphereDbContext _dbContext;
    private readonly SceneService _sceneService;

    public SceneServiceTests()
    {
        var options = new DbContextOptionsBuilder<HomeSphereDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HomeSphereDbContext(options);
        _sceneService = new SceneService(_dbContext);
    }

    [Fact]
    public async Task CreateScene_WhenDuplicateSlug_ShouldThrowConflictException()
    {
        var req1 = new CreateRoomSceneRequest(
            "Salón Serena", "salon-serena", "Desc", "https://img.com/1.jpg");
        await _sceneService.CreateSceneAsync(req1);

        var req2 = req1 with { Title = "Otro Salón" };
        await Assert.ThrowsAsync<ConflictException>(() => _sceneService.CreateSceneAsync(req2));
    }

    [Fact]
    public async Task AddHotspot_WhenXCoordinateExceeds100_ShouldThrowBusinessRuleValidationException()
    {
        var scene = await _sceneService.CreateSceneAsync(new CreateRoomSceneRequest(
            "Comedor", "comedor-sc", "Desc", "https://img.com/2.jpg"));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Mesa",
            Slug = "mesa",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 500m,
            Status = ProductStatus.Active
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var invalidHotspot = new CreateSceneHotspotRequest(
            product.Id, null, 105.50m, 50.00m, "Mesa Fuera de Rango");

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            _sceneService.AddHotspotAsync(scene.Id, invalidHotspot));

        Assert.Contains("between 0.00 and 100.00", ex.Message);
    }

    [Fact]
    public async Task AddHotspot_WhenYCoordinateNegative_ShouldThrowBusinessRuleValidationException()
    {
        var scene = await _sceneService.CreateSceneAsync(new CreateRoomSceneRequest(
            "Dormitorio", "dormitorio-sc", "Desc", "https://img.com/3.jpg"));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Cama",
            Slug = "cama",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 800m,
            Status = ProductStatus.Active
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var invalidHotspot = new CreateSceneHotspotRequest(
            product.Id, null, 50.00m, -5.00m, "Cama Negativa");

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            _sceneService.AddHotspotAsync(scene.Id, invalidHotspot));

        Assert.Contains("between 0.00 and 100.00", ex.Message);
    }

    [Fact]
    public async Task AddHotspot_WhenValidCoordinates_ShouldCreateAndMapHotspot()
    {
        var scene = await _sceneService.CreateSceneAsync(new CreateRoomSceneRequest(
            "Sala Nórdica", "sala-nordica", "Desc", "https://img.com/4.jpg"));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Sofá",
            Slug = "sofa",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 1200m,
            Status = ProductStatus.Active
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var validHotspot = new CreateSceneHotspotRequest(
            product.Id, null, 42.50m, 68.20m, "Sofá Principal");

        var hotspot = await _sceneService.AddHotspotAsync(scene.Id, validHotspot);

        Assert.NotNull(hotspot);
        Assert.Equal(42.50m, hotspot.XCoordinate);
        Assert.Equal(68.20m, hotspot.YCoordinate);
        Assert.Equal("Sofá Principal", hotspot.Title);
        Assert.Equal("sofa", hotspot.ProductSlug);
    }

    [Fact]
    public async Task GetScenes_ShouldIncludeRoomType()
    {
        var req = new CreateRoomSceneRequest(
            "Terraza Chill", "terraza-chill", "Espacio exterior con pérgola", "https://img.com/terraza.jpg",
            RoomType: "Terraza", DisplayOrder: 1, IsActive: true);

        await _sceneService.CreateSceneAsync(req);
        var scenes = await _sceneService.GetScenesAsync(onlyActive: false);

        var terraza = scenes.FirstOrDefault(s => s.Slug == "terraza-chill");
        Assert.NotNull(terraza);
        Assert.Equal("Terraza", terraza.RoomType);
    }

    [Fact]
    public async Task GetSceneBySlug_WhenProductInactive_ShouldMarkHotspotUnavailable()
    {
        var scene = await _sceneService.CreateSceneAsync(new CreateRoomSceneRequest(
            "Estudio Vintage", "estudio-vintage", "Espacio de trabajo", "https://img.com/estudio.jpg",
            RoomType: "Estudio"));

        var inactiveProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Escritorio Antiguo",
            Slug = "escritorio-antiguo",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 950m,
            Status = ProductStatus.Draft // Inactive
        };
        _dbContext.Products.Add(inactiveProduct);
        await _dbContext.SaveChangesAsync();

        await _sceneService.AddHotspotAsync(scene.Id, new CreateSceneHotspotRequest(
            inactiveProduct.Id, null, 50m, 50m, "Escritorio"));

        var detail = await _sceneService.GetSceneBySlugAsync(scene.Slug, onlyActive: false);

        Assert.NotNull(detail);
        Assert.Single(detail.Hotspots);
        Assert.False(detail.Hotspots[0].IsAvailable);
        Assert.Equal("Producto no disponible", detail.Hotspots[0].UnavailableReason);
    }

    [Fact]
    public async Task GetSceneBySlug_WhenVariantOutOfStock_ShouldMarkHotspotAgotado()
    {
        var scene = await _sceneService.CreateSceneAsync(new CreateRoomSceneRequest(
            "Lobby Hotel", "lobby-hotel", "Espacio de bienvenida", "https://img.com/lobby.jpg",
            RoomType: "Living"));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Sillón Club",
            Slug = "sillon-club",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 400m,
            Status = ProductStatus.Active
        };
        var outOfStockVariant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "SIL-CLUB-01",
            Name = "Cuero Marrón",
            Price = 450m,
            StockQuantity = 0, // Out of stock
            IsActive = true
        };
        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(outOfStockVariant);
        await _dbContext.SaveChangesAsync();

        await _sceneService.AddHotspotAsync(scene.Id, new CreateSceneHotspotRequest(
            product.Id, outOfStockVariant.Id, 25m, 40m, "Sillón Club"));

        var detail = await _sceneService.GetSceneBySlugAsync(scene.Slug, onlyActive: false);

        Assert.NotNull(detail);
        Assert.Single(detail.Hotspots);
        Assert.False(detail.Hotspots[0].IsAvailable);
        Assert.Equal("Agotado", detail.Hotspots[0].UnavailableReason);
    }

    [Fact]
    public async Task UpdateHotspot_WhenValidCoordinates_ShouldUpdateProperties()
    {
        var scene = await _sceneService.CreateSceneAsync(new CreateRoomSceneRequest(
            "Rincón de Lectura", "rincon-lectura", "Espacio acogedor", "https://img.com/reading.jpg",
            RoomType: "Living"));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Lámpara de Pie",
            Slug = "lampara-de-pie",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 180m,
            Status = ProductStatus.Active
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var created = await _sceneService.AddHotspotAsync(scene.Id, new CreateSceneHotspotRequest(
            product.Id, null, 20m, 30m, "Lámpara"));

        var updated = await _sceneService.UpdateHotspotAsync(scene.Id, created.Id, new UpdateSceneHotspotRequest(
            product.Id, null, 25.5m, 35.5m, "Lámpara Dorada", 2, true));

        Assert.Equal(25.5m, updated.XCoordinate);
        Assert.Equal(35.5m, updated.YCoordinate);
        Assert.Equal("Lámpara Dorada", updated.Title);
        Assert.Equal(2, updated.DisplayOrder);
    }

    [Fact]
    public async Task DeleteHotspot_ShouldRemoveHotspotFromScene()
    {
        var scene = await _sceneService.CreateSceneAsync(new CreateRoomSceneRequest(
            "Terraza Sol", "terraza-sol", "Terraza soleada", "https://img.com/sol.jpg",
            RoomType: "Terraza"));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Macetero",
            Slug = "macetero",
            ShortDescription = "Desc",
            Description = "Desc",
            BasePrice = 75m,
            Status = ProductStatus.Active
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var hotspot = await _sceneService.AddHotspotAsync(scene.Id, new CreateSceneHotspotRequest(
            product.Id, null, 15m, 80m, "Macetero"));

        await _sceneService.DeleteHotspotAsync(scene.Id, hotspot.Id);

        var detail = await _sceneService.GetSceneByIdAsync(scene.Id);
        Assert.Empty(detail.Hotspots);
    }
}

