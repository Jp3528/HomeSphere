using HomeSphere.Domain.Common;
using HomeSphere.Domain.Enums;

namespace HomeSphere.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? GuestEmail { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }

    public string? CouponCode { get; set; }
    public string ShippingMethod { get; set; } = "standard";

    public string ShippingAddressSnapshotJson { get; set; } = "{}";
    public string? BillingAddressSnapshotJson { get; set; }
    public string? CustomerNotes { get; set; }
    public string? CancellationReason { get; set; }

    public DateTime? PaidAtUtc { get; set; }
    public DateTime? ShippedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<StockReservation> Reservations { get; set; } = new List<StockReservation>();
    public ICollection<PaymentAttempt> PaymentAttempts { get; set; } = new List<PaymentAttempt>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
}
