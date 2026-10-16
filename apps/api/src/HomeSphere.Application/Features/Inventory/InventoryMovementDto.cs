using HomeSphere.Domain.Enums;

namespace HomeSphere.Application.Features.Inventory;

public record InventoryMovementDto(
    Guid Id,
    Guid VariantId,
    string Sku,
    string ProductName,
    InventoryMovementType Type,
    string TypeName,
    int QuantityDelta,
    int PreviousStock,
    int NewStock,
    string Reason,
    string? ReferenceType,
    string? ReferenceId,
    string? Actor,
    DateTime CreatedAtUtc
);
