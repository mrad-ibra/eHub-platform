using System.Collections.Concurrent;
using eHub.Application.Payments.Abstractions;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryPaymentWebhookInboxStore : IPaymentWebhookInboxStore
{
    private sealed class Row
    {
        public Guid? PaymentId { get; set; }
        public string Status { get; set; } = PaymentWebhookInboxStatuses.Received;
        public DateTime? ProcessedAtUtc { get; set; }
        public string? FailureReason { get; set; }
    }

    private readonly ConcurrentDictionary<string, Row> _rows = new(StringComparer.Ordinal);

    public Task<bool> TryBeginAsync(
        string provider,
        string providerEventId,
        string payloadHash,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_rows.TryAdd(Key(provider, providerEventId), new Row()));

    public Task CompleteAsync(
        string provider,
        string providerEventId,
        Guid? paymentId,
        string status,
        DateTime processedAtUtc,
        string? failureReason = null,
        CancellationToken cancellationToken = default)
    {
        if (_rows.TryGetValue(Key(provider, providerEventId), out var row))
        {
            row.PaymentId = paymentId;
            row.Status = status;
            row.ProcessedAtUtc = processedAtUtc;
            row.FailureReason = failureReason;
        }

        return Task.CompletedTask;
    }

    private static string Key(string provider, string eventId) => $"{provider}|{eventId}";
}
