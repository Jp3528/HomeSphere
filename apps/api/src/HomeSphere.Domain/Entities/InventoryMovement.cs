using HomeSphere.Domain.Common;
using HomeSphere.Domain.Enums;

namespace HomeSphere.Domain.Entities;

public class InventoryMovement : BaseEntity
{
    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    public InventoryMovementType Type { get; set; }
    public int QuantityDelta { get; set; } // + para entradas, - para salidas
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? ReferenceType { get; set; } // "Order", "ManualAdjustment", "Return"
    public string? ReferenceId { get; set; }   // e.g. OrderNumber o PublicId
    public string? Actor { get; set; }         // Email del usuario que realizó la acción
}
