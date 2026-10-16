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
}
