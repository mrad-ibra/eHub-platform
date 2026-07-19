using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Payments;

/// <summary>
/// Opaque provider identity in Domain. No Stripe/Iyzico/Payriff SDK types (L8).
/// Infrastructure ACL maps these codes to adapters.
/// </summary>
public sealed class PaymentProviderCode : IEquatable<PaymentProviderCode>
{
    private static readonly Dictionary<string, PaymentProviderCode> Known =
        new(StringComparer.Ordinal);

    public static readonly PaymentProviderCode Test = Register("TEST");
    public static readonly PaymentProviderCode Manual = Register("MANUAL");
    public static readonly PaymentProviderCode Stripe = Register("STRIPE");
    public static readonly PaymentProviderCode Payriff = Register("PAYRIFF");

    public string Value { get; }

    private PaymentProviderCode(string value) => Value = value;

    public static IReadOnlyCollection<PaymentProviderCode> All => Known.Values;

    public static PaymentProviderCode Parse(string value)
    {
        if (TryParse(value, out var code))
        {
            return code;
        }

        throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentProviderCodeInvalid));
    }

    public static bool TryParse(string? value, out PaymentProviderCode code)
    {
        code = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Known.TryGetValue(value.Trim().ToUpperInvariant(), out code!);
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj) => Equals(obj as PaymentProviderCode);

    public bool Equals(PaymentProviderCode? other)
        => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(PaymentProviderCode? left, PaymentProviderCode? right)
        => Equals(left, right);

    public static bool operator !=(PaymentProviderCode? left, PaymentProviderCode? right)
        => !Equals(left, right);

    public static implicit operator string(PaymentProviderCode code) => code.Value;

    private static PaymentProviderCode Register(string value)
    {
        var code = new PaymentProviderCode(value);
        Known.Add(value, code);
        return code;
    }
}
