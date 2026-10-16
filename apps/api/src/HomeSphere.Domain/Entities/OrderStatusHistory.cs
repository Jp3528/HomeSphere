using HomeSphere.Domain.Common;
using HomeSphere.Domain.Enums;

namespace HomeSphere.Domain.Entities;

public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public OrderStatus PreviousStatus { get; set; }
    public OrderStatus NewStatus { get; set; }

    public string ChangedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
