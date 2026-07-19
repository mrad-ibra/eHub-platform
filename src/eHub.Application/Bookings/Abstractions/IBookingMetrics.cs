namespace eHub.Application.Bookings.Abstractions;

/// <summary>Booking domain metrics (OpenTelemetry / Prometheus-friendly names).</summary>
public interface IBookingMetrics
{
    void BookingCreated();
    void BookingConflict();
    void IdempotencyConflict();
    void BookingExpired(int count);
    void ExpireWorkerDuration(TimeSpan duration);
    void ExpireWorkerFailed();
}
