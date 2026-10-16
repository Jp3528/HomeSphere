using HomeSphere.Application.Features.Analytics.DTOs;

namespace HomeSphere.Application.Features.Analytics;

public interface IAnalyticsService
{
    Task<DashboardKpisDto> GetDashboardKpisAsync(
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CancellationToken cancellationToken = default);
}
