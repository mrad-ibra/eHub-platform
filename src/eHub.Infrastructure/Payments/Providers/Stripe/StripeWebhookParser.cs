using System.Text.Json;
using System.Text.Json.Serialization;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;

namespace eHub.Infrastructure.Payments.Providers.Stripe;

/// <summary>
/// Parses Stripe webhook JSON after <see cref="StripeWebhookVerifier"/> has validated the signature.
/// Raw body is the Stripe Event envelope — no second ConstructEvent needed.
/// </summary>
public sealed class StripeWebhookParser(Application.Payments.Abstractions.IMinorUnitConverter minorUnits)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ProviderWebhookEvent? Parse(ReadOnlySpan<byte> rawBody)
    {
        StripeEventEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<StripeEventEnvelope>(rawBody, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (envelope is null || string.IsNullOrWhiteSpace(envelope.Id) || string.IsNullOrWhiteSpace(envelope.Type))
        {
            return null;
        }

        return Map(envelope);
    }

    private ProviderWebhookEvent Map(StripeEventEnvelope envelope)
    {
        var type = envelope.Type!.Trim().ToLowerInvariant();
        var data = envelope.Data?.Object;
        if (data is null)
        {
            return Unknown(envelope, null);
        }

        return type switch
        {
            "checkout.session.completed" => SessionOutcome(envelope, data, ProviderWebhookOutcome.Succeeded),
            "checkout.session.expired" => SessionOutcome(envelope, data, ProviderWebhookOutcome.Cancelled),
            "payment_intent.succeeded" => PaymentIntentOutcome(envelope, data, ProviderWebhookOutcome.Succeeded),
            "payment_intent.payment_failed" => PaymentIntentOutcome(
                envelope,
                data,
                ProviderWebhookOutcome.Failed,
                StripeFailureMapper.MapCode(data.LastPaymentError?.Code)),
            "payment_intent.canceled" => PaymentIntentOutcome(envelope, data, ProviderWebhookOutcome.Cancelled),
            "charge.refunded" or "charge.refund.updated" => RefundOutcome(envelope, data),
            _ => Unknown(envelope, data.Id)
        };
    }

    private ProviderWebhookEvent SessionOutcome(
        StripeEventEnvelope envelope,
        StripeObjectDto data,
        ProviderWebhookOutcome outcome)
    {
        var amount = TryAmount(data.AmountTotal, data.Currency);
        return new ProviderWebhookEvent(
            envelope.Id!.Trim(),
            data.Id,
            TryParseGuid(data.Metadata, "payment_id"),
            outcome,
            amount,
            null,
            null,
            null,
            ToUtc(envelope.Created));
    }

    private ProviderWebhookEvent PaymentIntentOutcome(
        StripeEventEnvelope envelope,
        StripeObjectDto data,
        ProviderWebhookOutcome outcome,
        PaymentFailureReason? failure = null)
    {
        var amount = TryAmount(data.Amount, data.Currency);
        return new ProviderWebhookEvent(
            envelope.Id!.Trim(),
            data.Id,
            TryParseGuid(data.Metadata, "payment_id"),
            outcome,
            amount,
            null,
            failure,
            null,
            ToUtc(envelope.Created));
    }

    private ProviderWebhookEvent RefundOutcome(StripeEventEnvelope envelope, StripeObjectDto data)
    {
        var refundAmount = TryAmount(data.AmountRefunded ?? data.Amount, data.Currency);
        return new ProviderWebhookEvent(
            envelope.Id!.Trim(),
            data.PaymentIntent ?? data.Id,
            TryParseGuid(data.Metadata, "payment_id"),
            ProviderWebhookOutcome.Refunded,
            null,
            null,
            null,
            refundAmount,
            ToUtc(envelope.Created));
    }

    private ProviderWebhookEvent Unknown(StripeEventEnvelope envelope, string? providerPaymentId)
        => new(
            envelope.Id!.Trim(),
            providerPaymentId,
            null,
            ProviderWebhookOutcome.Unknown,
            null,
            null,
            null,
            null,
            ToUtc(envelope.Created));

    private decimal? TryAmount(long? minor, string? currency)
    {
        if (minor is null || string.IsNullOrWhiteSpace(currency))
        {
            return null;
        }

        var code = currency.Trim().ToUpperInvariant();
        if (!minorUnits.IsSupported(code))
        {
            return null;
        }

        return minorUnits.FromMinorUnits(minor.Value, code);
    }

    private static Guid? TryParseGuid(Dictionary<string, string>? metadata, string key)
    {
        if (metadata is null || !metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static DateTime ToUtc(long? unix)
        => unix is null
            ? DateTime.UtcNow
            : DateTimeOffset.FromUnixTimeSeconds(unix.Value).UtcDateTime;

    private sealed class StripeEventEnvelope
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public long? Created { get; set; }
        public StripeEventData? Data { get; set; }
    }

    private sealed class StripeEventData
    {
        public StripeObjectDto? Object { get; set; }
    }

    private sealed class StripeObjectDto
    {
        public string? Id { get; set; }
        public string? Currency { get; set; }
        public long? Amount { get; set; }

        [JsonPropertyName("amount_total")]
        public long? AmountTotal { get; set; }

        [JsonPropertyName("amount_refunded")]
        public long? AmountRefunded { get; set; }

        [JsonPropertyName("payment_intent")]
        public string? PaymentIntent { get; set; }

        public Dictionary<string, string>? Metadata { get; set; }

        [JsonPropertyName("last_payment_error")]
        public StripeErrorDto? LastPaymentError { get; set; }
    }

    private sealed class StripeErrorDto
    {
        public string? Code { get; set; }
    }
}
