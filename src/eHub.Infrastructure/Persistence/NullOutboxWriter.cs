using eHub.Application.Bookings.Abstractions;
using eHub.Domain.Common;

namespace eHub.Infrastructure.Persistence;

/// <summary>No-op outbox for the in-memory (Sprint 5.1) persistence path.</summary>
public sealed class NullOutboxWriter : IOutboxWriter
{
    public Task EnqueueAsync(IDomainEvent domainEvent, DateTime nowUtc, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
