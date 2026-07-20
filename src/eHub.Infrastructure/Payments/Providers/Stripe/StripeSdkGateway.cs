using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using Stripe;
using Stripe.Checkout;

namespace eHub.Infrastructure.Payments.Providers.Stripe;

public sealed class StripeSdkGateway : IStripeGateway
{
    private readonly SessionService _sessions = new();
    private readonly RefundService _refunds = new();

    public async Task<StripeCreateSessionResult> CreateCheckoutSessionAsync(
        StripeCreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                ClientReferenceId = request.PaymentId.ToString("N"),
                Metadata = new Dictionary<string, string>
                {
                    ["payment_id"] = request.PaymentId.ToString("D"),
                    ["booking_id"] = request.BookingId.ToString("D")
                },
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = request.CurrencyCode.ToLowerInvariant(),
                            UnitAmount = request.AmountMinor,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Booking {request.BookingId:N}"
                            }
                        }
                    }
                ]
            };

            var session = await _sessions.CreateAsync(
                options,
                new RequestOptions { IdempotencyKey = request.IdempotencyKey },
                cancellationToken);

            return new StripeCreateSessionResult(
                true,
                session.Id,
                session.Url,
                null);
        }
        catch (StripeException ex)
        {
            return new StripeCreateSessionResult(false, null, null, StripeFailureMapper.FromException(ex));
        }
    }

    public async Task ExpireCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await _sessions.ExpireAsync(sessionId, cancellationToken: cancellationToken);
    }

    public async Task<string?> GetPaymentIntentIdForSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetAsync(sessionId, cancellationToken: cancellationToken);
        return session.PaymentIntentId;
    }

    public async Task<StripeRefundGatewayResult> CreateRefundAsync(
        StripeRefundGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refund = await _refunds.CreateAsync(
                new RefundCreateOptions
                {
                    PaymentIntent = request.PaymentIntentId,
                    Amount = request.AmountMinor,
                    Reason = "requested_by_customer",
                    Metadata = new Dictionary<string, string>
                    {
                        ["reason"] = request.Reason
                    }
                },
                new RequestOptions { IdempotencyKey = request.IdempotencyKey },
                cancellationToken);

            return new StripeRefundGatewayResult(true, refund.Id, null);
        }
        catch (StripeException ex)
        {
            return new StripeRefundGatewayResult(false, null, StripeFailureMapper.FromException(ex));
        }
    }

    public string? ConstructEventJson(
        string payload,
        string stripeSignatureHeader,
        string webhookSecret,
        long toleranceSeconds)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                stripeSignatureHeader,
                webhookSecret,
                toleranceSeconds,
                throwOnApiVersionMismatch: false);
            return stripeEvent.ToJson();
        }
        catch (StripeException)
        {
            return null;
        }
    }
}
