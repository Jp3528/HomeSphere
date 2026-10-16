namespace HomeSphere.Application.Features.Analytics.DTOs;

public record DashboardKpisDto(
    decimal TotalRevenue,
    int TotalOrders,
    decimal AverageTicket,
    int PaidOrdersCount,
    int PendingOrdersCount,
    int CompletedOrdersCount,
    int CancelledOrdersCount,
    int LowStockCount,
    int TotalActiveProducts,
    int TotalActiveScenes,
    IReadOnlyList<RevenueTrendDto> RevenueTrend,
    IReadOnlyList<OrdersByStatusDto> OrdersByStatus,
    IReadOnlyList<TopSellingProductDto> TopProducts,
    IReadOnlyList<LowStockAlertDto> LowStockAlerts
);

public record RevenueTrendDto(
    string Date,
    decimal Revenue,
    int OrdersCount
);

public record OrdersByStatusDto(
    string Status,
    int Count,
    decimal Percentage
);

public record TopSellingProductDto(
    Guid ProductId,
    string ProductName,
    string Sku,
    int UnitsSold,
    decimal RevenueGenerated,
    string? PrimaryImageUrl
);

public record LowStockAlertDto(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    Guid VariantId,
    string VariantName,
    string Sku,
    int StockQuantity,
    decimal Price
);
