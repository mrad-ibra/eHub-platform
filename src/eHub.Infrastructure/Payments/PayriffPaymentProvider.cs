using eHub.Application.Payments;

namespace eHub.Infrastructure.Payments;

/// <summary>
/// Payriff ACL skeleton. Webhook signature scheme TBD with Payriff docs (future).
/// Config: Payments:Providers:Payriff (MerchantId, SecretKey, WebhookSecret).
/// </summary>
public sealed class PayriffPaymentProvider : PaymentProviderSkeletonBase
{
    public override string ProviderKey => PaymentProviderCodes.Payriff;
}
