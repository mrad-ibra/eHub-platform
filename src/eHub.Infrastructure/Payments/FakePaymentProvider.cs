using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eHub.Application.Configuration;
using eHub.Application.Payments;
using eHub.Application.Payments.Abstractions;
using Microsoft.Extensions.Options;

namespace eHub.Infrastructure.Payments;

/// <summary>
/// Dev/test ACL adapter. No external SDK. Signature: HMAC-SHA256 over "{timestamp}.{body}".
/// Headers: X-EHub-Timestamp (unix seconds), X-EHub-Signature (hex digest).
/// </summary>
public sealed class FakePaymentProvider(IOptions<PaymentProviderOptions> options) : IPaymentProvider
{
    public const string SignatureHeader = "X-EHub-Signature";
    public const string TimestampHeader = "X-EHub-Timestamp";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<string, StoredCreate> _createByIdempotency = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, StoredRefund> _refundByIdempotency = new(StringComparer.Ordinal);

    public string ProviderKey => PaymentProviderCodes.Test;

    public Task<ProviderCreatePaymentResult> CreatePaymentAsync(
        ProviderCreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = request.IdempotencyKey.Trim();
        var fingerprint = CreateFingerprint.From(request);

        if (_createByIdempotency.TryGetValue(key, out var stored))
        {
            if (!stored.Fingerprint.Equals(fingerprint))
            {
                return Task.FromResult(ProviderCreatePaymentResult.Failed(IdempotencyMismatch()));
            }

            return Task.FromResult(ProviderCreatePaymentResult.Success(
                stored.ProviderPaymentId,
                stored.RedirectUrl));
        }

        var id = $"fake_{request.PaymentId:N}";
        var redirect = $"https://payments.ehub.local/fake/checkout/{id}";
        _createByIdempotency[key] = new StoredCreate(id, redirect, fingerprint);
        return Task.FromResult(ProviderCreatePaymentResult.Success(id, redirect));
    }

    public Task<ProviderCancelResult> CancelPaymentAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ProviderCancelResult.Success());
    }

    public Task<ProviderRefundResult> RefundAsync(
        ProviderRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = request.IdempotencyKey.Trim();
        var fingerprint = RefundFingerprint.From(request);

        if (_refundByIdempotency.TryGetValue(key, out var stored))
        {
            if (!stored.Fingerprint.Equals(fingerprint))
            {
                return Task.FromResult(ProviderRefundResult.Failed(IdempotencyMismatch()));
            }

            return Task.FromResult(ProviderRefundResult.Success(stored.ProviderRefundId));
        }

        var refundId = $"re_{Guid.NewGuid():N}";
        _refundByIdempotency[key] = new StoredRefund(refundId, fingerprint);
        return Task.FromResult(ProviderRefundResult.Success(refundId));
    }

    public bool VerifyWebhook(
        IReadOnlyDictionary<string, string> headers,
        ReadOnlySpan<byte> rawBody,
        DateTime nowUtc)
    {
        var cfg = options.Value.Fake;
        if (!TryGetHeader(headers, TimestampHeader, out var tsRaw)
            || !long.TryParse(tsRaw, out var unix))
        {
            return false;
        }

        var timestamp = DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
        var skew = TimeSpan.FromSeconds(Math.Max(30, cfg.TimestampToleranceSeconds));
        if (timestamp < nowUtc - skew || timestamp > nowUtc + skew)
        {
            return false;
        }

        if (!TryGetHeader(headers, SignatureHeader, out var signature))
        {
            return false;
        }

        var payload = $"{unix}.{Encoding.UTF8.GetString(rawBody)}";
        var expected = ComputeHmacHex(cfg.WebhookSecret, payload);
        try
        {
            var expectedBytes = Convert.FromHexString(expected);
            var actualBytes = Convert.FromHexString(signature.Trim());
            return expectedBytes.Length == actualBytes.Length
                && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public ProviderWebhookEvent? ParseWebhook(ReadOnlySpan<byte> rawBody)
    {
        FakeWebhookDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<FakeWebhookDto>(rawBody, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.EventId))
        {
            return null;
        }

        var failureReason = dto.FailureReason is null
            ? (PaymentFailureReason?)null
            : PaymentFailureReasonMapper.MapFakeWebhookFailure(dto.FailureReason);

        var outcome = dto.Outcome?.Trim().ToUpperInvariant() switch
        {
            "AUTHORIZED" => ProviderWebhookOutcome.Authorized,
            "SUCCEEDED" or "PAID" or "SUCCESS" => ProviderWebhookOutcome.Succeeded,
            "FAILED" or "DECLINED" => ProviderWebhookOutcome.Failed,
            "CANCELLED" or "CANCELED" => ProviderWebhookOutcome.Cancelled,
            "REFUNDED" => ProviderWebhookOutcome.Refunded,
            _ => ProviderWebhookOutcome.Unknown
        };

        return new ProviderWebhookEvent(
            dto.EventId.Trim(),
            dto.ProviderPaymentId,
            dto.PaymentId,
            outcome,
            dto.Amount,
            dto.CurrencyId,
            failureReason,
            dto.RefundAmount,
            dto.OccurredAtUtc ?? DateTime.UtcNow);
    }

    public static string Sign(string secret, long unixTimestamp, string body)
        => ComputeHmacHex(secret, $"{unixTimestamp}.{body}");

    private static ProviderFailure IdempotencyMismatch()
        => new(
            PaymentFailureReason.IdempotencyPayloadMismatch,
            ProviderCode: null,
            SafeMessage: null,
            IsRetryable: false);

    private static string ComputeHmacHex(string secret, string payload)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
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

    private sealed record CreateFingerprint(Guid PaymentId, Guid BookingId, decimal Amount, Guid CurrencyId)
    {
        public static CreateFingerprint From(ProviderCreatePaymentRequest request)
            => new(request.PaymentId, request.BookingId, request.Amount, request.CurrencyId);
    }

    private sealed record RefundFingerprint(
        string ProviderPaymentId,
        decimal Amount,
        Guid CurrencyId,
        string Reason)
    {
        public static RefundFingerprint From(ProviderRefundRequest request)
            => new(
                request.ProviderPaymentId.Trim(),
                request.Amount,
                request.CurrencyId,
                request.Reason.Trim());
    }

    private sealed record StoredCreate(
        string ProviderPaymentId,
        string RedirectUrl,
        CreateFingerprint Fingerprint);

    private sealed record StoredRefund(string ProviderRefundId, RefundFingerprint Fingerprint);

    private sealed class FakeWebhookDto
    {
        public string? EventId { get; set; }
        public string? ProviderPaymentId { get; set; }
        public Guid? PaymentId { get; set; }
        public string? Outcome { get; set; }
        public decimal? Amount { get; set; }
        public Guid? CurrencyId { get; set; }
        public string? FailureReason { get; set; }
        public decimal? RefundAmount { get; set; }
        public DateTime? OccurredAtUtc { get; set; }
    }
}
