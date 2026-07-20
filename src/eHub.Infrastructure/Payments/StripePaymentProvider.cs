using eHub.Application.Payments;

namespace eHub.Infrastructure.Payments;

/// <summary>
/// Stripe ACL skeleton. Webhook: Stripe-Signature header + construct event (future).
/// Config: Payments:Providers:Stripe (ApiKey, WebhookSecret).
/// </summary>
public sealed class StripePaymentProvider : PaymentProviderSkeletonBase
{
    public override string ProviderKey => PaymentProviderCodes.Stripe;
}
