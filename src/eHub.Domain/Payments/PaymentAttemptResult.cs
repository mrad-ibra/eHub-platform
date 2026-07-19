using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Payments;

public sealed class PaymentAttemptResult : IEquatable<PaymentAttemptResult>
{
    private static readonly Dictionary<string, PaymentAttemptResult> Known =
        new(StringComparer.Ordinal);

    public static readonly PaymentAttemptResult Succeeded = Register("SUCCEEDED");
    public static readonly PaymentAttemptResult Failed = Register("FAILED");
    public static readonly PaymentAttemptResult Pending = Register("PENDING");
    public static readonly PaymentAttemptResult Ignored = Register("IGNORED");

    public string Value { get; }

    private PaymentAttemptResult(string value) => Value = value;

    public static PaymentAttemptResult Parse(string value)
    {
        if (TryParse(value, out var result))
        {
            return result;
        }

        throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentAttemptResultInvalid));
    }

    public static bool TryParse(string? value, out PaymentAttemptResult result)
    {
        result = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Known.TryGetValue(value.Trim().ToUpperInvariant(), out result!);
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj) => Equals(obj as PaymentAttemptResult);

    public bool Equals(PaymentAttemptResult? other)
        => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static implicit operator string(PaymentAttemptResult result) => result.Value;

    private static PaymentAttemptResult Register(string value)
    {
        var result = new PaymentAttemptResult(value);
        Known.Add(value, result);
        return result;
    }
}
