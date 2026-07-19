using eHub.Application.Bookings.Abstractions;
using eHub.Application.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eHub.Infrastructure.Jobs;

/// <summary>Background loop for <see cref="ExpirePendingBookingsProcessor"/> (multi-instance safe via AggregateVersion).</summary>
public sealed class ExpirePendingBookingsHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<JobsOptions> options,
    ILogger<ExpirePendingBookingsHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cfg = options.Value.ExpirePendingBookings;
        if (!cfg.Enabled)
        {
            logger.LogInformation("ExpirePendingBookings job is disabled");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(5, cfg.IntervalSeconds));
        var retryDelay = TimeSpan.FromSeconds(Math.Max(5, cfg.RetryDelaySeconds));

        logger.LogInformation(
            "ExpirePendingBookings job started (interval={Interval}s, batch={Batch})",
            interval.TotalSeconds,
            cfg.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ExpirePendingBookingsProcessor>();
                await processor.RunOnceAsync(stoppingToken);
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ExpirePendingBookings tick failed; backing off {RetryDelay}", retryDelay);
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    scope.ServiceProvider.GetRequiredService<IBookingMetrics>().ExpireWorkerFailed();
                }
                catch
                {
                    // metrics must not break backoff
                }

                try
                {
                    await Task.Delay(retryDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
