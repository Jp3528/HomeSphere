using HomeSphere.Application.Features.Orders;

namespace HomeSphere.WebApi.BackgroundServices;

public class ExpiredReservationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredReservationWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public ExpiredReservationWorker(
        IServiceProvider serviceProvider,
        ILogger<ExpiredReservationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExpiredReservationWorker background service started.");

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var cleanedCount = await orderService.CleanupExpiredReservationsAsync(stoppingToken);

                if (cleanedCount > 0)
                {
                    _logger.LogInformation("ExpiredReservationWorker: Automatically released stock for {Count} expired orders.", cleanedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing ExpiredReservationWorker cycle.");
            }
        }

        _logger.LogInformation("ExpiredReservationWorker background service stopped.");
    }
}
