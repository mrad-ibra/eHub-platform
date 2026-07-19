using eHub.Application.Bookings.Abstractions;
using Microsoft.Extensions.Logging;

namespace eHub.Infrastructure.Jobs;

public sealed class LoggingExpireBookingsMetrics(ILogger<LoggingExpireBookingsMetrics> logger)
    : IExpireBookingsMetrics
{
    public void RecordBatch(int expiredCount, int skippedCount, TimeSpan duration)
    {
        if (expiredCount == 0 && skippedCount == 0)
        {
            return;
        }

        logger.LogDebug(
            "ExpireBookings metrics: expired={Expired} skipped={Skipped} durationMs={DurationMs}",
            expiredCount,
            skippedCount,
            (int)duration.TotalMilliseconds);
    }
}
