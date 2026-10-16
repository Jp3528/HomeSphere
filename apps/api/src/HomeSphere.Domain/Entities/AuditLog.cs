using HomeSphere.Domain.Common;

namespace HomeSphere.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string? ActorEmail { get; set; }
    public string? ActorIpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public string? DetailsJson { get; set; }
}
