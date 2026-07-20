namespace eHub.Application.Configuration;

public sealed class PaymentProviderOptions
{
    public const string SectionName = "Payments:Providers";

    public FakeProviderOptions Fake { get; set; } = new();
}

public sealed class FakeProviderOptions
{
    public string WebhookSecret { get; set; } = "ehub-fake-webhook-secret-change-me";
    public int TimestampToleranceSeconds { get; set; } = 300;
}
