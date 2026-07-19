using eHub.Domain.Common;

namespace eHub.Application.Bookings.Abstractions;

/// <summary>
/// Persists domain events in the same unit-of-work as the aggregate write (outbox pattern).
/// </summary>
public interface IOutboxWriter
{
    Task EnqueueAsync(IDomainEvent domainEvent, DateTime nowUtc, CancellationToken cancellationToken = default);
}
