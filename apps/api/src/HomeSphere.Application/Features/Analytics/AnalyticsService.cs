using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Analytics.DTOs;
using HomeSphere.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Features.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly IApplicationDbContext _context;

    // Recognized revenue statuses
    private static readonly OrderStatus[] RecognizedRevenueStatuses =
    [
        OrderStatus.Paid,
        OrderStatus.Processing,
        OrderStatus.Shipped,
        OrderStatus.Delivered
    ];

    public AnalyticsService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardKpisDto> GetDashboardKpisAsync(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var end = toUtc ?? DateTimeOffset.UtcNow;
        var start = fromUtc ?? end.AddDays(-30);

        // 1. Fetch Orders in Period
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CreatedAtUtc >= start && o.CreatedAtUtc <= end)
            .ToListAsync(cancellationToken);

        var totalOrders = orders.Count;

        var recognizedOrders = orders
            .Where(o => RecognizedRevenueStatuses.Contains(o.Status))
            .ToList();

        var totalRevenue = recognizedOrders.Sum(o => o.Total);
        var paidOrdersCount = recognizedOrders.Count;
        var averageTicket = paidOrdersCount > 0 ? Math.Round(totalRevenue / paidOrdersCount, 2) : 0m;

        var pendingOrdersCount = orders.Count(o => o.Status == OrderStatus.PendingPayment);
        var completedOrdersCount = orders.Count(o => o.Status == OrderStatus.Delivered);
        var cancelledOrdersCount = orders.Count(o => o.Status == OrderStatus.Cancelled);

        // 2. Orders by Status Breakdown
        var ordersByStatus = Enum.GetValues<OrderStatus>()
            .Select(status =>
            {
                var count = orders.Count(o => o.Status == status);
                var percentage = totalOrders > 0
                    ? Math.Round(((decimal)count / totalOrders) * 100m, 1)
                    : 0m;

                return new OrdersByStatusDto(
                    status.ToString(),
                    count,
                    percentage
                );
            })
            .Where(s => s.Count > 0)
            .OrderByDescending(s => s.Count)
            .ToList();

        // 3. Daily Revenue Trend (Last 30 Days)
        var dailyBuckets = new Dictionary<string, (decimal Revenue, int Count)>();
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            dailyBuckets[date.ToString("yyyy-MM-dd")] = (0m, 0);
        }

        foreach (var order in recognizedOrders)
        {
            var dateKey = order.CreatedAtUtc.ToString("yyyy-MM-dd");
            if (dailyBuckets.ContainsKey(dateKey))
            {
                var current = dailyBuckets[dateKey];
                dailyBuckets[dateKey] = (current.Revenue + order.Total, current.Count + 1);
            }
        }

        var revenueTrend = dailyBuckets
            .Select(kvp => new RevenueTrendDto(kvp.Key, Math.Round(kvp.Value.Revenue, 2), kvp.Value.Count))
            .OrderBy(r => r.Date)
            .ToList();

        // 4. Top Selling Products
        var recognizedItems = recognizedOrders
            .SelectMany(o => o.Items)
            .ToList();

        var topProducts = recognizedItems
            .GroupBy(i => new { i.ProductVariantId, i.ProductNameSnapshot, i.SkuSnapshot })
            .Select(g => new
            {
                VariantId = g.Key.ProductVariantId,
                ProductName = g.Key.ProductNameSnapshot,
                Sku = g.Key.SkuSnapshot,
                UnitsSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice)
            })
            .OrderByDescending(x => x.UnitsSold)
            .Take(5)
            .Select(x => new TopSellingProductDto(
                x.VariantId,
                x.ProductName,
                x.Sku,
                x.UnitsSold,
                Math.Round(x.Revenue, 2),
                null
            ))
            .ToList();

        // 5. Low Stock Alerts (StockQuantity <= 5)
        var lowStockVariants = await _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Where(v => v.IsActive && v.Product.Status == ProductStatus.Active && v.StockQuantity <= 5)
            .OrderBy(v => v.StockQuantity)
            .Take(10)
            .Select(v => new LowStockAlertDto(
                v.ProductId,
                v.Product.Name,
                v.Product.Slug,
                v.Id,
                v.Name,
                v.Sku,
                v.StockQuantity,
                v.Price
            ))
            .ToListAsync(cancellationToken);

        var totalActiveProducts = await _context.Products
            .CountAsync(p => p.Status == ProductStatus.Active, cancellationToken);

        var totalActiveScenes = await _context.RoomScenes
            .CountAsync(s => s.IsActive, cancellationToken);

        return new DashboardKpisDto(
            Math.Round(totalRevenue, 2),
            totalOrders,
            averageTicket,
            paidOrdersCount,
            pendingOrdersCount,
            completedOrdersCount,
            cancelledOrdersCount,
            lowStockVariants.Count,
            totalActiveProducts,
            totalActiveScenes,
            revenueTrend,
            ordersByStatus,
            topProducts,
            lowStockVariants
        );
    }
}
