namespace HomeSphere.Application.Features.Inventory;

public record InventoryItemDto(
    Guid VariantId,
    Guid ProductId,
    string Sku,
    string VariantName,
    string ProductName,
    string CategoryName,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    bool IsLowStock,
    DateTime? LastMovementUtc
);
