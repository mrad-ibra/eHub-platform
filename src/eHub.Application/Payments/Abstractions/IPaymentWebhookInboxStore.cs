namespace eHub.Application.Payments.Abstractions;

public interface IPaymentWebhookInboxStore
{
    /// <summary>Insert inbox row. Returns false when (Provider, EventId) already exists.</summary>
    Task<bool> TryBeginAsync(
        string provider,
        string providerEventId,
        string payloadHash,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        string provider,
        string providerEventId,
        Guid? paymentId,
        string status,
        DateTime processedAtUtc,
        string? failureReason = null,
        CancellationToken cancellationToken = default);
}
