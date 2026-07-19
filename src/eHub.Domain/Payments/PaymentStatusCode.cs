using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Payments;

/// <summary>Smart enum for Payment status (Sprint 6.0 state machine).</summary>
public sealed class PaymentStatusCode : IEquatable<PaymentStatusCode>
{
    private static readonly Dictionary<string, PaymentStatusCode> Known =
        new(StringComparer.Ordinal);

    public static readonly PaymentStatusCode Created = Register("CREATED");
    public static readonly PaymentStatusCode Pending = Register("PENDING");
    public static readonly PaymentStatusCode Authorized = Register("AUTHORIZED");
    public static readonly PaymentStatusCode Succeeded = Register("SUCCEEDED");
    public static readonly PaymentStatusCode Failed = Register("FAILED");
    public static readonly PaymentStatusCode Cancelled = Register("CANCELLED");
    public static readonly PaymentStatusCode Expired = Register("EXPIRED");
    public static readonly PaymentStatusCode PartiallyRefunded = Register("PARTIALLY_REFUNDED");
    public static readonly PaymentStatusCode Refunded = Register("REFUNDED");

    public string Value { get; }

    private PaymentStatusCode(string value) => Value = value;

    public static IReadOnlyCollection<PaymentStatusCode> All => Known.Values;

    public bool IsTerminal => IsOneOf(Failed, Cancelled, Expired, Refunded);

    public bool AllowsRefund => IsOneOf(Succeeded, PartiallyRefunded);

    public bool CanBecomeSucceeded => IsOneOf(Pending, Authorized);

    public static PaymentStatusCode Parse(string value)
    {
        if (TryParse(value, out var code))
        {
            return code;
        }

        throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentStatusCodeInvalid));
    }

    public static bool TryParse(string? value, out PaymentStatusCode code)
    {
        code = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Known.TryGetValue(value.Trim().ToUpperInvariant(), out code!);
    }

    public bool IsOneOf(params PaymentStatusCode[] statuses)
        => statuses.Any(status => Equals(status));

    public override string ToString() => Value;

    public override bool Equals(object? obj) => Equals(obj as PaymentStatusCode);

    public bool Equals(PaymentStatusCode? other)
        => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(PaymentStatusCode? left, PaymentStatusCode? right)
        => Equals(left, right);

    public static bool operator !=(PaymentStatusCode? left, PaymentStatusCode? right)
        => !Equals(left, right);

    public static implicit operator string(PaymentStatusCode code) => code.Value;

    private static PaymentStatusCode Register(string value)
    {
        var code = new PaymentStatusCode(value);
        Known.Add(value, code);
        return code;
    }
}
