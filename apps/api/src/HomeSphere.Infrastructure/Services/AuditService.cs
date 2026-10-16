using System.Text.Json;
using System.Text.Json.Nodes;
using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeSphere.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwordhash",
        "token",
        "refreshtoken",
        "secret",
        "apikey",
        "cardnumber",
        "cvv",
        "authorization"
    };

    public AuditService(IApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActionAsync(
        string action,
        string entityName,
        string entityId,
        object? details = null,
        string? actorId = null,
        string? actorEmail = null,
        string? ipAddress = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string? sanitizedJson = null;

            if (details != null)
            {
                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var serialized = JsonSerializer.Serialize(details, options);
                var node = JsonNode.Parse(serialized);
                if (node != null)
                {
                    SanitizeJsonNode(node);
                    sanitizedJson = node.ToJsonString(options);
                }
            }

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                Action = action.Trim().ToUpperInvariant(),
                EntityName = entityName.Trim(),
                EntityId = entityId.Trim(),
                ActorId = actorId,
                ActorEmail = actorEmail,
                ActorIpAddress = ipAddress,
                CorrelationId = correlationId,
                DetailsJson = sanitizedJson,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Non-blocking logging failure
            _logger.LogError(ex, "Failed to persist audit log for Action: {Action}, Entity: {EntityName}", action, entityName);
        }
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetRecentLogsAsync(
        int limit = 50,
        string? entityName = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAtUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(a => a.EntityName == entityName.Trim());
        }

        return await query
            .Take(Math.Min(limit, 200))
            .Select(a => new AuditLogDto(
                a.Id,
                a.Action,
                a.EntityName,
                a.EntityId,
                a.ActorId,
                a.ActorEmail,
                a.ActorIpAddress,
                a.CorrelationId,
                a.DetailsJson,
                a.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);
    }

    private static void SanitizeJsonNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            var keysToRedact = new List<string>();

            foreach (var kvp in obj)
            {
                if (SensitiveKeys.Contains(kvp.Key))
                {
                    keysToRedact.Add(kvp.Key);
                }
                else if (kvp.Value != null)
                {
                    SanitizeJsonNode(kvp.Value);
                }
            }

            foreach (var key in keysToRedact)
            {
                obj[key] = "[REDACTED]";
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item != null)
                {
                    SanitizeJsonNode(item);
                }
            }
        }
    }
}
