namespace eHub.Application.Payments;

/// <summary>String constants for provider routing (mirrors Domain <c>PaymentProviderCode</c> values).</summary>
public static class PaymentProviderCodes
{
    public const string Test = "TEST";
    public const string Manual = "MANUAL";
    public const string Stripe = "STRIPE";
    public const string Payriff = "PAYRIFF";
}
