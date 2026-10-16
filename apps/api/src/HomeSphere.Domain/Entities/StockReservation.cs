using HomeSphere.Domain.Common;
using HomeSphere.Domain.Enums;

namespace HomeSphere.Domain.Entities;

public class StockReservation : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductVariantId { get; set; }
    public ProductVariant ProductVariant { get; set; } = null!;

    public int Quantity { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
}
