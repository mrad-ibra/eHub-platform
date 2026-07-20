using System.Text;
using eHub.Application.Configuration;
using Microsoft.Extensions.Options;

namespace eHub.Infrastructure.Payments.Providers.Stripe;

public sealed class StripeWebhookVerifier(
    IStripeGateway gateway,
    IOptions<PaymentProviderOptions> options)
{
    public const string SignatureHeader = "Stripe-Signature";

    public bool Verify(
        IReadOnlyDictionary<string, string> headers,
        ReadOnlySpan<byte> rawBody,
        DateTime nowUtc)
    {
        _ = nowUtc;
        var cfg = options.Value.Stripe;
        if (string.IsNullOrWhiteSpace(cfg.WebhookSecret))
        {
            return false;
        }

        if (!TryGetHeader(headers, SignatureHeader, out var signature))
        {
            return false;
        }

        var payload = Encoding.UTF8.GetString(rawBody);
        var json = gateway.ConstructEventJson(
            payload,
            signature,
            cfg.WebhookSecret,
            Math.Max(30, cfg.WebhookToleranceSeconds));
        return json is not null;
    }

    private static bool TryGetHeader(
        IReadOnlyDictionary<string, string> headers,
        string name,
        out string value)
    {
        foreach (var pair in headers)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(pair.Value))
            {
                value = pair.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}
