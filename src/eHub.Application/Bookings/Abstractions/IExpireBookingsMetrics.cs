namespace eHub.Application.Bookings.Abstractions;

public interface IExpireBookingsMetrics
{
    void RecordBatch(int expiredCount, int skippedCount, TimeSpan duration);
}
