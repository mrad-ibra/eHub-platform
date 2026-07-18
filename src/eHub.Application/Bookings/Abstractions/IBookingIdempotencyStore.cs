namespace eHub.Application.Bookings.Abstractions;

public enum BookingIdempotencyStatus
{
    Started = 0,
    Completed = 1
}

public sealed record BookingIdempotencyRecord(
    Guid UserId,
    string Key,
    string RequestHash,
    BookingIdempotencyStatus Status,
    Guid? BookingId,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);

public abstract record IdempotencyBeginResult
{
    public sealed record Began(BookingIdempotencyRecord Record) : IdempotencyBeginResult;

    public sealed record CompletedReplay(Guid BookingId) : IdempotencyBeginResult;

    public sealed record PayloadMismatch : IdempotencyBeginResult;

    public sealed record InProgress : IdempotencyBeginResult;
}

public interface IBookingIdempotencyStore
{
    /// <summary>
    /// Atomically claims (UserId, Key). Same hash + completed → replay.
    /// Same key + different hash → mismatch. Never overwrites a completed record.
    /// </summary>
    Task<IdempotencyBeginResult> BeginAsync(
        Guid userId,
        string idempotencyKey,
        string requestHash,
        DateTime nowUtc,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid userId,
        string idempotencyKey,
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task AbandonAsync(
        Guid userId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
