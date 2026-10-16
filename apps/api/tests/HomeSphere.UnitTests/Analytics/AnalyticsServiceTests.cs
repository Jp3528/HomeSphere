using HomeSphere.Application.Features.Analytics;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.UnitTests.Analytics;

public class AnalyticsServiceTests
{
    private readonly HomeSphereDbContext _dbContext;
    private readonly AnalyticsService _analyticsService;

    public AnalyticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<HomeSphereDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HomeSphereDbContext(options);
        _analyticsService = new AnalyticsService(_dbContext);
    }

    [Fact]
    public async Task GetDashboardKpis_WhenNoOrders_ShouldReturnZeroRevenue()
    {
        var kpis = await _analyticsService.GetDashboardKpisAsync();

        Assert.Equal(0m, kpis.TotalRevenue);
        Assert.Equal(0, kpis.TotalOrders);
        Assert.Equal(0m, kpis.AverageTicket);
        Assert.Empty(kpis.TopProducts);
    }

    [Fact]
    public async Task GetDashboardKpis_ShouldRecognizeRevenueOnlyFromPaidStatuses()
    {
        var now = DateTime.UtcNow;

        var paidOrder = new Order
        {
            OrderNumber = "ORD-001",
            PublicId = "ord_001",
            Status = OrderStatus.Paid,
            Total = 200m,
            Subtotal = 200m,
            CreatedAtUtc = now.AddDays(-2)
        };

        var deliveredOrder = new Order
        {
            OrderNumber = "ORD-002",
            PublicId = "ord_002",
            Status = OrderStatus.Delivered,
            Total = 300m,
            Subtotal = 300m,
            CreatedAtUtc = now.AddDays(-1)
        };

        var pendingOrder = new Order
        {
            OrderNumber = "ORD-003",
            PublicId = "ord_003",
            Status = OrderStatus.PendingPayment,
            Total = 150m,
            Subtotal = 150m,
            CreatedAtUtc = now
        };

        var cancelledOrder = new Order
        {
            OrderNumber = "ORD-004",
            PublicId = "ord_004",
            Status = OrderStatus.Cancelled,
            Total = 400m,
            Subtotal = 400m,
            CreatedAtUtc = now.AddDays(-3)
        };

        _dbContext.Orders.AddRange(paidOrder, deliveredOrder, pendingOrder, cancelledOrder);
        await _dbContext.SaveChangesAsync();

        var kpis = await _analyticsService.GetDashboardKpisAsync();

        // Total orders is 4, but recognized revenue is only from Paid (200) + Delivered (300) = 500
        Assert.Equal(4, kpis.TotalOrders);
        Assert.Equal(500m, kpis.TotalRevenue);
        Assert.Equal(2, kpis.PaidOrdersCount);
        Assert.Equal(250m, kpis.AverageTicket); // 500 / 2
        Assert.Equal(1, kpis.PendingOrdersCount);
        Assert.Equal(1, kpis.CancelledOrdersCount);
    }

    [Fact]
    public async Task GetDashboardKpis_ShouldIdentifyLowStockVariants()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Mesa Roble",
            Slug = "mesa-roble",
            Status = ProductStatus.Active,
            BasePrice = 600m
        };
        _dbContext.Products.Add(product);

        var lowStockVariant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "ROB-LOW-01",
            Name = "Roble 160cm",
            Price = 600m,
            StockQuantity = 2, // Low stock <= 5
            IsActive = true
        };

        var normalStockVariant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Sku = "ROB-NORM-02",
            Name = "Roble 200cm",
            Price = 750m,
            StockQuantity = 20,
            IsActive = true
        };

        _dbContext.ProductVariants.AddRange(lowStockVariant, normalStockVariant);
        await _dbContext.SaveChangesAsync();

        var kpis = await _analyticsService.GetDashboardKpisAsync();

        Assert.Equal(1, kpis.LowStockCount);
        Assert.Single(kpis.LowStockAlerts);
        Assert.Equal("ROB-LOW-01", kpis.LowStockAlerts[0].Sku);
        Assert.Equal(2, kpis.LowStockAlerts[0].StockQuantity);
    }

    [Fact]
    public async Task GetDashboardKpis_ShouldAggregateTopSellingProducts()
    {
        var now = DateTime.UtcNow;
        var variantId1 = Guid.NewGuid();
        var variantId2 = Guid.NewGuid();

        var order = new Order
        {
            OrderNumber = "ORD-TOP-1",
            PublicId = "ord_top_1",
            Status = OrderStatus.Delivered,
            Total = 1100m,
            CreatedAtUtc = now.AddDays(-1),
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductVariantId = variantId1,
                    ProductNameSnapshot = "Lámpara Minimalista",
                    SkuSnapshot = "LAMP-01",
                    UnitPrice = 100m,
                    Quantity = 5,
                    TotalPrice = 500m
                },
                new()
                {
                    ProductVariantId = variantId2,
                    ProductNameSnapshot = "Silla Tapizada",
                    SkuSnapshot = "SILLA-02",
                    UnitPrice = 200m,
                    Quantity = 3,
                    TotalPrice = 600m
                }
            }
        };

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        var kpis = await _analyticsService.GetDashboardKpisAsync();

        Assert.Equal(2, kpis.TopProducts.Count);
        Assert.Equal("LAMP-01", kpis.TopProducts[0].Sku);
        Assert.Equal(5, kpis.TopProducts[0].UnitsSold);
        Assert.Equal(500m, kpis.TopProducts[0].RevenueGenerated);

        Assert.Equal("SILLA-02", kpis.TopProducts[1].Sku);
        Assert.Equal(3, kpis.TopProducts[1].UnitsSold);
        Assert.Equal(600m, kpis.TopProducts[1].RevenueGenerated);
    }
}
