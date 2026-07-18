using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Assets;

/// <summary>
/// Smart enum / value object for Asset lifecycle status.
/// Values align with Catalog <c>AssetStatus</c> codes.
/// </summary>
public sealed class AssetStatusCode : IEquatable<AssetStatusCode>
{
    private static readonly Dictionary<string, AssetStatusCode> Known =
        new(StringComparer.Ordinal);

    public static readonly AssetStatusCode Draft = Register("DRAFT");
    public static readonly AssetStatusCode PendingApproval = Register("PENDING_APPROVAL");
    public static readonly AssetStatusCode Published = Register("PUBLISHED");
    public static readonly AssetStatusCode Suspended = Register("SUSPENDED");
    public static readonly AssetStatusCode Archived = Register("ARCHIVED");
    public static readonly AssetStatusCode Rejected = Register("REJECTED");

    public string Value { get; }

    private AssetStatusCode(string value) => Value = value;

    public static IReadOnlyCollection<AssetStatusCode> All => Known.Values;

    public static AssetStatusCode Parse(string value)
    {
        if (TryParse(value, out var code))
        {
            return code;
        }

        throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.AssetStatusCodeInvalid));
    }

    public static bool TryParse(string? value, out AssetStatusCode code)
    {
        code = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Known.TryGetValue(value.Trim().ToUpperInvariant(), out code!);
    }

    public bool IsOneOf(params AssetStatusCode[] statuses)
        => statuses.Any(status => Equals(status));

    public override string ToString() => Value;

    public override bool Equals(object? obj) => Equals(obj as AssetStatusCode);

    public bool Equals(AssetStatusCode? other)
        => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(AssetStatusCode? left, AssetStatusCode? right)
        => Equals(left, right);

    public static bool operator !=(AssetStatusCode? left, AssetStatusCode? right)
        => !Equals(left, right);

    public static implicit operator string(AssetStatusCode code) => code.Value;

    private static AssetStatusCode Register(string value)
    {
        var code = new AssetStatusCode(value);
        Known.Add(value, code);
        return code;
    }
}
