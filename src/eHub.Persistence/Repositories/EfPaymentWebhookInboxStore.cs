using eHub.Application.Payments.Abstractions;
using eHub.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace eHub.Persistence.Repositories;

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
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Provider == provider && x.ProviderEventId == providerEventId,
                cancellationToken);
        if (existing is not null)
        {
            if (existing.Status == PaymentWebhookInboxStatuses.Received)
            {
                return true;
            }

            return false;
        }

        db.PaymentWebhookInbox.Add(new PaymentWebhookInbox
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ProviderEventId = providerEventId,
            PayloadHash = payloadHash,
            Status = PaymentWebhookInboxStatuses.Received,
            ReceivedAtUtc = nowUtc
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var raced = await db.PaymentWebhookInbox
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Provider == provider && x.ProviderEventId == providerEventId,
                    cancellationToken);
            if (raced?.Status == PaymentWebhookInboxStatuses.Received)
            {
                return true;
            }

            return false;
        }
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
        await db.SaveChangesAsync(cancellationToken);
    }
}
