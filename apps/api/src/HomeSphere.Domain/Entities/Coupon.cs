using HomeSphere.Domain.Common;

namespace HomeSphere.Domain.Entities;

public enum DiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset ValidUntilUtc { get; set; }
    public int? UsageLimit { get; set; }
    public int TimesUsed { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
