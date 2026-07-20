using eHub.Domain.Common;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Payments;

public sealed class RefundStatusCode : IEquatable<RefundStatusCode>
{
    private static readonly Dictionary<string, RefundStatusCode> Known =
        new(StringComparer.Ordinal);

    public static readonly RefundStatusCode Requested = Register("REQUESTED");
    public static readonly RefundStatusCode Succeeded = Register("SUCCEEDED");
    public static readonly RefundStatusCode Failed = Register("FAILED");

    public string Value { get; }

    private RefundStatusCode(string value) => Value = value;

    public static RefundStatusCode Parse(string value)
    {
        if (TryParse(value, out var code))
        {
            return code;
        }

        throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentRefundStatusInvalid));
    }

    public static bool TryParse(string? value, out RefundStatusCode code)
    {
        code = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Known.TryGetValue(value.Trim().ToUpperInvariant(), out code!);
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj) => Equals(obj as RefundStatusCode);

    public bool Equals(RefundStatusCode? other)
        => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static implicit operator string(RefundStatusCode code) => code.Value;

    private static RefundStatusCode Register(string value)
    {
        var code = new RefundStatusCode(value);
        Known.Add(value, code);
        return code;
    }
}

/// <summary>Audited refund child of Payment (L5).</summary>
public sealed class Refund
{
    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public string Reason { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? ProviderRefundId { get; private set; }
    public Guid? RequestedByActorId { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? SettledAtUtc { get; private set; }

    private Refund()
    {
    }

    internal static Refund Request(
        Guid paymentId,
        Money amount,
        string reason,
        string idempotencyKey,
        DateTime nowUtc,
        Guid? actorId)
    {
        if (amount.Amount <= 0)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentRefundAmountInvalid));
        }

        var key = AppGuard.NotEmpty(idempotencyKey, nameof(idempotencyKey)).Trim();
        if (key.Length > PaymentDefaults.MaxIdempotencyKeyLength)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentIdempotencyKeyInvalid));
        }

        var trimmed = AppGuard.NotEmpty(reason, nameof(reason)).Trim();
        if (trimmed.Length > PaymentDefaults.MaxRefundReasonLength)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentRefundReasonTooLong));
        }

        return new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = paymentId,
            Amount = amount,
            Reason = trimmed,
            IdempotencyKey = key,
            Status = RefundStatusCode.Requested,
            RequestedByActorId = actorId,
            RequestedAtUtc = nowUtc
        };
    }

    internal void MarkSucceeded(DateTime settledAtUtc, string? providerRefundId)
    {
        if (Status != RefundStatusCode.Requested.Value)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentInvalidStatusTransition));
        }

        Status = RefundStatusCode.Succeeded;
        SettledAtUtc = settledAtUtc;
        if (!string.IsNullOrWhiteSpace(providerRefundId))
        {
            var id = providerRefundId.Trim();
            ProviderRefundId = id.Length <= PaymentDefaults.MaxProviderPaymentIdLength
                ? id
                : id[..PaymentDefaults.MaxProviderPaymentIdLength];
        }
    }

    internal void MarkFailed(DateTime atUtc)
    {
        if (Status != RefundStatusCode.Requested.Value)
        {
            throw new ConflictException(ErrorResources.Get(ErrorCodes.PaymentInvalidStatusTransition));
        }

        Status = RefundStatusCode.Failed;
        SettledAtUtc = atUtc;
    }

    public bool MatchesIdempotentPayload(decimal amount, Guid currencyId, string reason)
        => Amount.Amount == amount
            && Amount.CurrencyId == currencyId
            && string.Equals(Reason, reason.Trim(), StringComparison.Ordinal);
}
