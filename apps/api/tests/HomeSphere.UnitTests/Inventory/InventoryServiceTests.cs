using HomeSphere.Application.Features.Inventory;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using HomeSphere.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.UnitTests.Inventory;

public class InventoryServiceTests
{
    private readonly HomeSphereDbContext _dbContext;
    private readonly InventoryService _inventoryService;

    public InventoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<HomeSphereDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HomeSphereDbContext(options);
        _inventoryService = new InventoryService(_dbContext);
    }

    private ProductVariant CreateTestVariant(string sku, int initialStock, decimal price = 150m)
    {
        var category = new Category { Name = "Comedor", Slug = "comedor" };
        var brand = new Brand { Name = "Stockholm", Slug = "stockholm" };
        var product = new Product
        {
            Name = "Mesa Comedor",
            Slug = "mesa-comedor",
            BasePrice = price,
            Category = category,
            Brand = brand
        };

        var variant = new ProductVariant
        {
            Sku = sku,
            Name = "Roble 6 puestos",
            Price = price,
            StockQuantity = initialStock,
            IsActive = true,
            Product = product
        };

        _dbContext.Products.Add(product);
        _dbContext.ProductVariants.Add(variant);
        return variant;
    }

    [Fact]
    public async Task AdjustStock_WhenPositiveDelta_ShouldIncreaseStockAndRecordMovement()
    {
        var variant = CreateTestVariant("SKU-POS-01", 10);
        await _dbContext.SaveChangesAsync();

        var request = new AdjustStockRequest(
            VariantId: variant.Id,
            QuantityDelta: 5,
            Type: InventoryMovementType.Receipt,
            Reason: "Ingreso de nuevo lote desde proveedor"
        );

        var result = await _inventoryService.AdjustStockAsync(request, "almacen@homesphere.com");

        Assert.Equal(15, result.NewStock);
        Assert.Equal(10, result.PreviousStock);
        Assert.Equal(5, result.QuantityDelta);
        Assert.Equal("almacen@homesphere.com", result.Actor);

        var variantInDb = await _dbContext.ProductVariants.FindAsync(variant.Id);
        Assert.NotNull(variantInDb);
        Assert.Equal(15, variantInDb.StockQuantity);

        var movementInDb = await _dbContext.InventoryMovements.FirstOrDefaultAsync(m => m.ProductVariantId == variant.Id);
        Assert.NotNull(movementInDb);
        Assert.Equal(InventoryMovementType.Receipt, movementInDb.Type);
    }

    [Fact]
    public async Task AdjustStock_WhenNegativeDeltaWithinBounds_ShouldDecreaseStock()
    {
        var variant = CreateTestVariant("SKU-NEG-01", 10);
        await _dbContext.SaveChangesAsync();

        var request = new AdjustStockRequest(
            VariantId: variant.Id,
            QuantityDelta: -4,
            Type: InventoryMovementType.Adjustment,
            Reason: "Merma por daño en transporte"
        );

        var result = await _inventoryService.AdjustStockAsync(request, "control@homesphere.com");

        Assert.Equal(6, result.NewStock);
        Assert.Equal(10, result.PreviousStock);
        Assert.Equal(-4, result.QuantityDelta);

        var variantInDb = await _dbContext.ProductVariants.FindAsync(variant.Id);
        Assert.Equal(6, variantInDb!.StockQuantity);
    }

    [Fact]
    public async Task AdjustStock_WhenNegativeDeltaExceedsStock_ShouldThrowBusinessRuleValidationException_AntiOverselling()
    {
        var variant = CreateTestVariant("SKU-OVERSOLD-01", 5);
        await _dbContext.SaveChangesAsync();

        var request = new AdjustStockRequest(
            VariantId: variant.Id,
            QuantityDelta: -10,
            Type: InventoryMovementType.Adjustment,
            Reason: "Intento de descontar más de lo disponible"
        );

        var ex = await Assert.ThrowsAsync<BusinessRuleValidationException>(() =>
            _inventoryService.AdjustStockAsync(request));

        Assert.Contains("negativo", ex.Message);

        // Stock remains unchanged
        var variantInDb = await _dbContext.ProductVariants.FindAsync(variant.Id);
        Assert.Equal(5, variantInDb!.StockQuantity);
    }

    [Fact]
    public async Task AdjustStock_WhenVariantNotFound_ShouldThrowNotFoundException()
    {
        var request = new AdjustStockRequest(
            VariantId: Guid.NewGuid(),
            QuantityDelta: 1,
            Type: InventoryMovementType.Receipt,
            Reason: "Inexistente"
        );

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _inventoryService.AdjustStockAsync(request));
    }

    [Fact]
    public async Task GetInventoryItems_ShouldFlagLowStockCorrectly()
    {
        var v1 = CreateTestVariant("SKU-LOW", 3);
        var v2 = CreateTestVariant("SKU-OK", 20);
        await _dbContext.SaveChangesAsync();

        var items = await _inventoryService.GetInventoryItemsAsync();

        var item1 = items.First(i => i.VariantId == v1.Id);
        var item2 = items.First(i => i.VariantId == v2.Id);

        Assert.True(item1.IsLowStock);
        Assert.False(item2.IsLowStock);
    }

    [Fact]
    public async Task GetMovements_ShouldFilterByVariantId()
    {
        var v1 = CreateTestVariant("SKU-M1", 10);
        var v2 = CreateTestVariant("SKU-M2", 15);
        await _dbContext.SaveChangesAsync();

        await _inventoryService.AdjustStockAsync(new AdjustStockRequest(v1.Id, 2, InventoryMovementType.Receipt, "Lote 1"));
        await _inventoryService.AdjustStockAsync(new AdjustStockRequest(v2.Id, 3, InventoryMovementType.Receipt, "Lote 2"));

        var v1Movements = await _inventoryService.GetMovementsAsync(v1.Id);

        Assert.Single(v1Movements);
        Assert.Equal(v1.Id, v1Movements[0].VariantId);
    }
}
