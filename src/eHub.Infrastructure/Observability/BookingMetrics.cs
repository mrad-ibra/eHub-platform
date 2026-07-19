using System.Diagnostics.Metrics;
using eHub.Application.Bookings.Abstractions;

namespace eHub.Infrastructure.Observability;

public sealed class BookingMetrics : IBookingMetrics
{
    public const string MeterName = "eHub.Bookings";

    private readonly Counter<long> _created;
    private readonly Counter<long> _conflict;
    private readonly Counter<long> _idempotencyConflict;
    private readonly Counter<long> _expired;
    private readonly Histogram<double> _expireDuration;
    private readonly Counter<long> _expireFailed;

    public BookingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _created = meter.CreateCounter<long>("booking_created_total");
        _conflict = meter.CreateCounter<long>("booking_conflict_total");
        _idempotencyConflict = meter.CreateCounter<long>("idempotency_conflict_total");
        _expired = meter.CreateCounter<long>("booking_expired_total");
        _expireDuration = meter.CreateHistogram<double>("expire_worker_duration", unit: "ms");
        _expireFailed = meter.CreateCounter<long>("expire_worker_failed_total");
    }

    public void BookingCreated() => _created.Add(1);
    public void BookingConflict() => _conflict.Add(1);
    public void IdempotencyConflict() => _idempotencyConflict.Add(1);
    public void BookingExpired(int count)
    {
        if (count > 0)
        {
            _expired.Add(count);
        }
    }

    public void ExpireWorkerDuration(TimeSpan duration) => _expireDuration.Record(duration.TotalMilliseconds);
    public void ExpireWorkerFailed() => _expireFailed.Add(1);
}
