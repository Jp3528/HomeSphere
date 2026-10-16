namespace HomeSphere.Application.Common.Interfaces;

public record AuditLogDto(
    Guid Id,
    string Action,
    string EntityName,
    string EntityId,
    string? ActorId,
    string? ActorEmail,
    string? ActorIpAddress,
    string? CorrelationId,
    string? DetailsJson,
    DateTime CreatedAtUtc
);

public interface IAuditService
{
    Task LogActionAsync(
        string action,
        string entityName,
        string entityId,
        object? details = null,
        string? actorId = null,
        string? actorEmail = null,
        string? ipAddress = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogDto>> GetRecentLogsAsync(
        int limit = 50,
        string? entityName = null,
        CancellationToken cancellationToken = default);
}
