namespace eHub.Application.Configuration;

public sealed class PaymentProviderOptions
{
    public const string SectionName = "Payments:Providers";

    public FakeProviderOptions Fake { get; set; } = new();
    public StripeProviderOptions Stripe { get; set; } = new();
    public PayriffProviderOptions Payriff { get; set; } = new();
}

public sealed class StripeProviderOptions
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
    public int WebhookToleranceSeconds { get; set; } = 300;
}

public sealed class FakeProviderOptions
{
    /// <summary>When false, Fake/TEST adapter is not registered (production should leave this false).</summary>
    public bool Enabled { get; set; } = true;
    public string WebhookSecret { get; set; } = "ehub-fake-webhook-secret-change-me";
    public int TimestampToleranceSeconds { get; set; } = 300;
}

public sealed class PayriffProviderOptions
{
    public bool Enabled { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
