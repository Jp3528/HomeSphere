using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = Role.Admin)]
public class AdminAuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AdminAuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int limit = 50,
        [FromQuery] string? entityName = null,
        CancellationToken cancellationToken = default)
    {
        var logs = await _auditService.GetRecentLogsAsync(limit, entityName, cancellationToken);
        return Ok(logs);
    }
}
