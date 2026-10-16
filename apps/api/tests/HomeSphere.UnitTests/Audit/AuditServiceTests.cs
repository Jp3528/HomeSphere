using HomeSphere.Infrastructure.Persistence;
using HomeSphere.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HomeSphere.UnitTests.Audit;

public class AuditServiceTests
{
    private readonly HomeSphereDbContext _dbContext;
    private readonly AuditService _auditService;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<HomeSphereDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HomeSphereDbContext(options);
        _auditService = new AuditService(_dbContext, NullLogger<AuditService>.Instance);
    }

    [Fact]
    public async Task LogAction_ShouldRedactSensitiveFieldsInDetailsJson()
    {
        var sensitivePayload = new
        {
            Username = "admin_user",
            Password = "SuperSecretPassword123!",
            Token = "jwt_bearer_token_xyz",
            CardNumber = "4111111111111111",
            Cvv = "123",
            SafeNote = "Actualización administrativa regular"
        };

        await _auditService.LogActionAsync(
            "SECURITY_TEST",
            "User",
            "user-123",
            details: sensitivePayload,
            actorId: "admin-1",
            actorEmail: "admin@homesphere.com");

        var logs = await _auditService.GetRecentLogsAsync(limit: 10);

        Assert.Single(logs);
        var log = logs[0];
        Assert.NotNull(log.DetailsJson);

        Assert.DoesNotContain("SuperSecretPassword123!", log.DetailsJson);
        Assert.DoesNotContain("jwt_bearer_token_xyz", log.DetailsJson);
        Assert.DoesNotContain("4111111111111111", log.DetailsJson);
        Assert.DoesNotContain("123", log.DetailsJson);

        Assert.Contains("[REDACTED]", log.DetailsJson);
        Assert.Contains("Actualización administrativa regular", log.DetailsJson);
        Assert.Equal("SECURITY_TEST", log.Action);
    }

    [Fact]
    public async Task GetRecentLogs_ShouldFilterByEntityName()
    {
        await _auditService.LogActionAsync("CREATE", "RoomScene", "scene-1");
        await _auditService.LogActionAsync("UPDATE", "Order", "order-1");
        await _auditService.LogActionAsync("DELETE", "RoomScene", "scene-2");

        var sceneLogs = await _auditService.GetRecentLogsAsync(entityName: "RoomScene");
        var orderLogs = await _auditService.GetRecentLogsAsync(entityName: "Order");

        Assert.Equal(2, sceneLogs.Count);
        Assert.All(sceneLogs, l => Assert.Equal("RoomScene", l.EntityName));

        Assert.Single(orderLogs);
        Assert.Equal("Order", orderLogs[0].EntityName);
    }
}
