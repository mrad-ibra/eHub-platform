using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Bookings;

/// <summary>Smart enum aligned with Catalog BookingStatus codes.</summary>
public sealed class BookingStatusCode : IEquatable<BookingStatusCode>
{
    private static readonly Dictionary<string, BookingStatusCode> Known =
        new(StringComparer.Ordinal);

    public static readonly BookingStatusCode Draft = Register("DRAFT");
    public static readonly BookingStatusCode PendingOwnerApproval = Register("PENDING_OWNER_APPROVAL");
    public static readonly BookingStatusCode PendingPayment = Register("PENDING_PAYMENT");
    public static readonly BookingStatusCode Confirmed = Register("CONFIRMED");
    public static readonly BookingStatusCode Rejected = Register("REJECTED");
    public static readonly BookingStatusCode Cancelled = Register("CANCELLED");
    public static readonly BookingStatusCode Expired = Register("EXPIRED");
    public static readonly BookingStatusCode InProgress = Register("IN_PROGRESS");
    public static readonly BookingStatusCode Completed = Register("COMPLETED");
    public static readonly BookingStatusCode Refunded = Register("REFUNDED");

    public string Value { get; }

    private BookingStatusCode(string value) => Value = value;

    public static IReadOnlyCollection<BookingStatusCode> All => Known.Values;

    public bool IsBlocking => IsOneOf(
        PendingOwnerApproval,
        PendingPayment,
        Confirmed,
        InProgress);

    public static BookingStatusCode Parse(string value)
    {
        if (TryParse(value, out var code))
        {
            return code;
        }

        throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.BookingStatusCodeInvalid));
    }

    public static bool TryParse(string? value, out BookingStatusCode code)
    {
        code = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Known.TryGetValue(value.Trim().ToUpperInvariant(), out code!);
    }

    public bool IsOneOf(params BookingStatusCode[] statuses)
        => statuses.Any(status => Equals(status));

    public override string ToString() => Value;

    public override bool Equals(object? obj) => Equals(obj as BookingStatusCode);

    public bool Equals(BookingStatusCode? other)
        => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(BookingStatusCode? left, BookingStatusCode? right)
        => Equals(left, right);

    public static bool operator !=(BookingStatusCode? left, BookingStatusCode? right)
        => !Equals(left, right);

    public static implicit operator string(BookingStatusCode code) => code.Value;

    private static BookingStatusCode Register(string value)
    {
        var code = new BookingStatusCode(value);
        Known.Add(value, code);
        return code;
    }
}
