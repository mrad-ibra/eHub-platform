using eHub.Application.Configuration;
using eHub.Application.Payments.Abstractions;
using eHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace eHub.Persistence.Repositories;

/// <summary>
/// Inbox rows are staged in the same <see cref="EHubDbContext"/> as payments/outbox and committed
/// via <see cref="Application.Common.Persistence.IUnitOfWork"/> (single transaction).
/// <c>Received</c> rows older than the processing lease may be reclaimed after a crash.
/// </summary>
public sealed class EfPaymentWebhookInboxStore(EHubDbContext db) : IPaymentWebhookInboxStore
{
    public async Task<bool> TryBeginAsync(
        string provider,
        string providerEventId,
        string payloadHash,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.PaymentWebhookInbox
            .FirstOrDefaultAsync(
                x => x.Provider == provider && x.ProviderEventId == providerEventId,
                cancellationToken);

        if (existing is null)
        {
            db.PaymentWebhookInbox.Add(new PaymentWebhookInbox
            {
                Id = Guid.NewGuid(),
                Provider = provider,
                ProviderEventId = providerEventId,
                PayloadHash = payloadHash,
                Status = PaymentWebhookInboxStatuses.Received,
                ReceivedAtUtc = nowUtc
            });
            return true;
        }

        if (IsTerminal(existing.Status))
        {
            return false;
        }

        if (existing.Status == PaymentWebhookInboxStatuses.Received)
        {
            if (nowUtc - existing.ReceivedAtUtc <= PaymentWebhookInboxOptions.ProcessingLease)
            {
                return false;
            }

            existing.ReceivedAtUtc = nowUtc;
            existing.PayloadHash = payloadHash;
            return true;
        }

        return false;
    }

    public async Task CompleteAsync(
        string provider,
        string providerEventId,
        Guid? paymentId,
        string status,
        DateTime processedAtUtc,
        string? failureReason = null,
        CancellationToken cancellationToken = default)
    {
        var row = await db.PaymentWebhookInbox
            .FirstOrDefaultAsync(
                x => x.Provider == provider && x.ProviderEventId == providerEventId,
                cancellationToken);
        if (row is null)
        {
            return;
        }

        row.PaymentId = paymentId;
        row.Status = status;
        row.ProcessedAtUtc = processedAtUtc;
        row.FailureReason = failureReason;
    }

    private static bool IsTerminal(string status)
        => status is PaymentWebhookInboxStatuses.Processed
            or PaymentWebhookInboxStatuses.Ignored
            or PaymentWebhookInboxStatuses.Failed;
}
