namespace eHub.Application.Payments.Abstractions;

public interface IPaymentProvider
{
    string ProviderKey { get; }

    Task<ProviderCreatePaymentResult> CreatePaymentAsync(
        ProviderCreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task CancelPaymentAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default);

    Task<ProviderRefundResult> RefundAsync(
        ProviderRefundRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Verify signature + timestamp. Must use raw body bytes.</summary>
    bool VerifyWebhook(
        IReadOnlyDictionary<string, string> headers,
        ReadOnlySpan<byte> rawBody,
        DateTime nowUtc);

    /// <summary>Parse after verify. Returns null for unknown/ignored event types (safe ack).</summary>
    ProviderWebhookEvent? ParseWebhook(ReadOnlySpan<byte> rawBody);
}

public interface IPaymentProviderResolver
{
    IPaymentProvider GetRequired(string providerKey);
}

public sealed record ProviderCreatePaymentRequest(
    Guid PaymentId,
    Guid BookingId,
    decimal Amount,
    Guid CurrencyId,
    string IdempotencyKey);

public sealed record ProviderCreatePaymentResult(
    string ProviderPaymentId,
    string? RedirectUrl);

public sealed record ProviderRefundRequest(
    string ProviderPaymentId,
    decimal Amount,
    Guid CurrencyId,
    string IdempotencyKey,
    string Reason);

public sealed record ProviderRefundResult(
    string? ProviderRefundId,
    bool Succeeded,
    string? FailureReason);

public enum ProviderWebhookOutcome
{
    Authorized,
    Succeeded,
    Failed,
    Cancelled,
    Refunded,
    Unknown
}

public sealed record ProviderWebhookEvent(
    string EventId,
    string? ProviderPaymentId,
    Guid? PaymentId,
    ProviderWebhookOutcome Outcome,
    decimal? Amount,
    Guid? CurrencyId,
    string? FailureReason,
    decimal? RefundAmount,
    DateTime OccurredAtUtc);
