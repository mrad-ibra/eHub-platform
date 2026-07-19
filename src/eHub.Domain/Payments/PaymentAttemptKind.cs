using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Payments;

public sealed class PaymentAttemptKind : IEquatable<PaymentAttemptKind>
{
    private static readonly Dictionary<string, PaymentAttemptKind> Known =
        new(StringComparer.Ordinal);

    public static readonly PaymentAttemptKind Create = Register("CREATE");
    public static readonly PaymentAttemptKind Webhook = Register("WEBHOOK");
    public static readonly PaymentAttemptKind Capture = Register("CAPTURE");
    public static readonly PaymentAttemptKind Refund = Register("REFUND");
    public static readonly PaymentAttemptKind Reconcile = Register("RECONCILE");
    public static readonly PaymentAttemptKind Cancel = Register("CANCEL");

    public string Value { get; }

    private PaymentAttemptKind(string value) => Value = value;

    public static PaymentAttemptKind Parse(string value)
    {
        if (TryParse(value, out var kind))
        {
            return kind;
        }

        throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PaymentAttemptKindInvalid));
    }

    public static bool TryParse(string? value, out PaymentAttemptKind kind)
    {
        kind = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Known.TryGetValue(value.Trim().ToUpperInvariant(), out kind!);
    }

    public override string ToString() => Value;

    public override bool Equals(object? obj) => Equals(obj as PaymentAttemptKind);

    public bool Equals(PaymentAttemptKind? other)
        => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static implicit operator string(PaymentAttemptKind kind) => kind.Value;

    private static PaymentAttemptKind Register(string value)
    {
        var kind = new PaymentAttemptKind(value);
        Known.Add(value, kind);
        return kind;
    }
}
