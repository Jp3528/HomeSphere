using HomeSphere.Application.Features.Analytics;
using HomeSphere.Application.Features.Analytics.DTOs;
using HomeSphere.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = $"{Role.Admin},{Role.Manager}")]
public class AdminAnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AdminAnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardKpisDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardKpis(
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
    {
        var kpis = await _analyticsService.GetDashboardKpisAsync(fromUtc, toUtc, cancellationToken);
        return Ok(kpis);
    }
}
