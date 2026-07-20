using eHub.Application.Payments.Abstractions;

namespace eHub.Infrastructure.Payments;

/// <summary>
/// Placeholder ACL adapter. Registered for routing and webhook endpoint discovery;
/// SDK wiring lands in a later sprint. No provider SDK types here (L8).
/// </summary>
public abstract class PaymentProviderSkeletonBase : IPaymentProvider
{
    public abstract string ProviderKey { get; }

    public Task<ProviderCreatePaymentResult> CreatePaymentAsync(
        ProviderCreatePaymentRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromException<ProviderCreatePaymentResult>(NotWired(nameof(CreatePaymentAsync)));

    public Task CancelPaymentAsync(string providerPaymentId, CancellationToken cancellationToken = default)
        => Task.FromException(NotWired(nameof(CancelPaymentAsync)));

    public Task<ProviderRefundResult> RefundAsync(
        ProviderRefundRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromException<ProviderRefundResult>(NotWired(nameof(RefundAsync)));

    public bool VerifyWebhook(
        IReadOnlyDictionary<string, string> headers,
        ReadOnlySpan<byte> rawBody,
        DateTime nowUtc)
        => false;

    public ProviderWebhookEvent? ParseWebhook(ReadOnlySpan<byte> rawBody)
        => null;

    private InvalidOperationException NotWired(string operation)
        => new(
            $"{ProviderKey} payment provider is registered but not yet wired. " +
            $"Operation '{operation}' will be implemented when the {ProviderKey} SDK adapter is enabled.");
}
