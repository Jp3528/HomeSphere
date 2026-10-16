using HomeSphere.Domain.Common;

namespace HomeSphere.Domain.Entities;

public class Cart : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? GuestToken { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
