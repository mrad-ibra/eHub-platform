using eHub.Application.Configuration;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using Microsoft.Extensions.Options;
using Stripe;

namespace eHub.Infrastructure.Payments.Providers.Stripe;

public sealed class StripePaymentProvider(
    IStripeGateway gateway,
    StripeWebhookVerifier verifier,
    StripeWebhookParser parser,
    IMinorUnitConverter minorUnits,
    IOptions<PaymentProviderOptions> options) : IPaymentProvider
{
    public string ProviderKey => PaymentProviderCodes.Stripe;

    public async Task<ProviderCreatePaymentResult> CreatePaymentAsync(
        ProviderCreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cfg = options.Value.Stripe;
        if (string.IsNullOrWhiteSpace(cfg.SuccessUrl) || string.IsNullOrWhiteSpace(cfg.CancelUrl))
        {
            return ProviderCreatePaymentResult.Failed(new ProviderFailure(
                PaymentFailureReason.InvalidRequest,
                ProviderCode: "missing_redirect_urls",
                SafeMessage: null,
                IsRetryable: false));
        }

        if (!minorUnits.IsSupported(request.CurrencyCode))
        {
            return ProviderCreatePaymentResult.Failed(new ProviderFailure(
                PaymentFailureReason.InvalidRequest,
                ProviderCode: "unsupported_currency",
                SafeMessage: null,
                IsRetryable: false));
        }

        var result = await gateway.CreateCheckoutSessionAsync(
            new StripeCreateSessionRequest(
                request.PaymentId,
                request.BookingId,
                minorUnits.ToMinorUnits(request.Amount, request.CurrencyCode),
                request.CurrencyCode,
                request.IdempotencyKey,
                AppendPaymentId(cfg.SuccessUrl, request.PaymentId),
                AppendPaymentId(cfg.CancelUrl, request.PaymentId)),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return ProviderCreatePaymentResult.Failed(
                result.Failure ?? new ProviderFailure(
                    PaymentFailureReason.Unknown,
                    null,
                    null,
                    false));
        }

        return ProviderCreatePaymentResult.Success(result.SessionId!, result.RedirectUrl);
    }

    public async Task<ProviderCancelResult> CancelPaymentAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await gateway.ExpireCheckoutSessionAsync(providerPaymentId, cancellationToken);
            return ProviderCancelResult.Success();
        }
        catch (StripeException ex)
        {
            return ProviderCancelResult.Failed(StripeFailureMapper.FromException(ex));
        }
    }

    public async Task<ProviderRefundResult> RefundAsync(
        ProviderRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!minorUnits.IsSupported(request.CurrencyCode))
        {
            return ProviderRefundResult.Failed(new ProviderFailure(
                PaymentFailureReason.InvalidRequest,
                ProviderCode: "unsupported_currency",
                SafeMessage: null,
                IsRetryable: false));
        }

        try
        {
            var paymentIntentId = await gateway.GetPaymentIntentIdForSessionAsync(
                request.ProviderPaymentId,
                cancellationToken);
            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                // ProviderPaymentId may already be a PaymentIntent id (webhook path).
                paymentIntentId = request.ProviderPaymentId;
            }

            var result = await gateway.CreateRefundAsync(
                new StripeRefundGatewayRequest(
                    paymentIntentId!,
                    minorUnits.ToMinorUnits(request.Amount, request.CurrencyCode),
                    request.IdempotencyKey,
                    request.Reason),
                cancellationToken);

            if (!result.IsSuccess)
            {
                return ProviderRefundResult.Failed(
                    result.Failure ?? new ProviderFailure(
                        PaymentFailureReason.Unknown,
                        null,
                        null,
                        false));
            }

            return ProviderRefundResult.Success(result.RefundId!);
        }
        catch (StripeException ex)
        {
            return ProviderRefundResult.Failed(StripeFailureMapper.FromException(ex));
        }
    }

    public bool VerifyWebhook(
        IReadOnlyDictionary<string, string> headers,
        ReadOnlySpan<byte> rawBody,
        DateTime nowUtc)
        => verifier.Verify(headers, rawBody, nowUtc);

    public ProviderWebhookEvent? ParseWebhook(ReadOnlySpan<byte> rawBody)
        => parser.Parse(rawBody);

    private static string AppendPaymentId(string urlTemplate, Guid paymentId)
    {
        var url = urlTemplate.Trim();
        if (url.Contains("{PAYMENT_ID}", StringComparison.OrdinalIgnoreCase))
        {
            return url.Replace("{PAYMENT_ID}", paymentId.ToString("D"), StringComparison.OrdinalIgnoreCase);
        }

        var separator = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{url}{separator}paymentId={paymentId:D}";
    }
}
