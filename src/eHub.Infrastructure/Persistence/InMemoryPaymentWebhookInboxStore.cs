using System.Collections.Concurrent;
using eHub.Application.Configuration;
using eHub.Application.Payments.Abstractions;

namespace eHub.Infrastructure.Persistence;

public sealed class InMemoryPaymentWebhookInboxStore : IPaymentWebhookInboxStore
{
    private sealed class Row
    {
        public Guid? PaymentId { get; set; }
        public string Status { get; set; } = PaymentWebhookInboxStatuses.Received;
        public DateTime ReceivedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public string? FailureReason { get; set; }
        public string PayloadHash { get; set; } = string.Empty;
    }

    private readonly ConcurrentDictionary<string, Row> _rows = new(StringComparer.Ordinal);

    public Task<bool> TryBeginAsync(
        string provider,
        string providerEventId,
        string payloadHash,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var key = Key(provider, providerEventId);

        if (_rows.TryGetValue(key, out var existing))
        {
            if (IsTerminal(existing.Status))
            {
                return Task.FromResult(false);
            }

            if (existing.Status == PaymentWebhookInboxStatuses.Received)
            {
                if (nowUtc - existing.ReceivedAtUtc <= PaymentWebhookInboxOptions.ProcessingLease)
                {
                    return Task.FromResult(false);
                }

                existing.ReceivedAtUtc = nowUtc;
                existing.PayloadHash = payloadHash;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        return Task.FromResult(_rows.TryAdd(key, new Row
        {
            ReceivedAtUtc = nowUtc,
            PayloadHash = payloadHash
        }));
    }

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

    private static bool IsTerminal(string status)
        => status is PaymentWebhookInboxStatuses.Processed
            or PaymentWebhookInboxStatuses.Ignored
            or PaymentWebhookInboxStatuses.Failed;

    private static string Key(string provider, string eventId) => $"{provider}|{eventId}";
}
