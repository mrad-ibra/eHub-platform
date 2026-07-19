using System.Text.Json;
using eHub.Application.Bookings.Abstractions;
using eHub.Domain.Common;
using eHub.Persistence.Entities;

namespace eHub.Persistence;

public sealed class EfOutboxWriter(EHubDbContext db) : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task EnqueueAsync(IDomainEvent domainEvent, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().Name,
            PayloadJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions),
            OccurredAtUtc = nowUtc,
            CreatedAtUtc = nowUtc,
            ProcessedAtUtc = null,
            AttemptCount = 0
        });

        return Task.CompletedTask;
    }
}
