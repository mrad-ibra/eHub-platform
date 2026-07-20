namespace eHub.Application.Payments.Abstractions;

public interface IPaymentProvider
{
    string ProviderKey { get; }

    Task<ProviderCreatePaymentResult> CreatePaymentAsync(
        ProviderCreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<ProviderCancelResult> CancelPaymentAsync(
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
    bool IsSuccess,
    string? ProviderPaymentId,
    string? RedirectUrl,
    ProviderFailure? Failure)
{
    public static ProviderCreatePaymentResult Success(string providerPaymentId, string? redirectUrl = null)
        => new(true, providerPaymentId, redirectUrl, null);

    public static ProviderCreatePaymentResult Failed(ProviderFailure failure)
        => new(false, null, null, failure);
}

public sealed record ProviderRefundRequest(
    string ProviderPaymentId,
    decimal Amount,
    Guid CurrencyId,
    string IdempotencyKey,
    string Reason);

public sealed record ProviderRefundResult(
    bool IsSuccess,
    string? ProviderRefundId,
    ProviderFailure? Failure)
{
    public static ProviderRefundResult Success(string providerRefundId)
        => new(true, providerRefundId, null);

    public static ProviderRefundResult Failed(ProviderFailure failure)
        => new(false, null, failure);
}

public sealed record ProviderCancelResult(
    bool IsSuccess,
    ProviderFailure? Failure)
{
    public static ProviderCancelResult Success() => new(true, null);

    public static ProviderCancelResult Failed(ProviderFailure failure) => new(false, failure);
}

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
