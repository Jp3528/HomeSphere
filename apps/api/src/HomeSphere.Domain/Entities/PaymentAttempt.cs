using HomeSphere.Domain.Common;
using HomeSphere.Domain.Enums;

namespace HomeSphere.Domain.Entities;

public class PaymentAttempt : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public string Provider { get; set; } = "Mock";
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentAttemptStatus Status { get; set; } = PaymentAttemptStatus.Initiated;
    public string? ErrorMessage { get; set; }
    public string? RawPayloadJson { get; set; }
}
