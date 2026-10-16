using System.ComponentModel.DataAnnotations;
using HomeSphere.Domain.Enums;

namespace HomeSphere.Application.Features.Inventory;

public record AdjustStockRequest(
    [Required] Guid VariantId,
    [Required] int QuantityDelta,
    [Required] InventoryMovementType Type,
    [Required, MinLength(3)] string Reason,
    string? ReferenceType = null,
    string? ReferenceId = null
);
