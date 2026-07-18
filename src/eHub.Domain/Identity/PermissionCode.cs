using eHub.Domain.Exceptions;
using eHub.Domain.Resources;

namespace eHub.Domain.Identity;

/// <summary>
/// Strongly-typed permission code in <c>module.action</c> form (e.g. identity.users.read).
/// </summary>
public readonly record struct PermissionCode
{
    public string Value { get; }

    private PermissionCode(string value) => Value = value;

    public static PermissionCode Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PermissionCodeRequired));
        }

        var normalized = code.Trim().ToLowerInvariant();
        var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PermissionCodeInvalidFormat));
        }

        if (parts.Any(string.IsNullOrWhiteSpace))
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.PermissionCodeEmptySegment));
        }

        return new PermissionCode(normalized);
    }

    public override string ToString() => Value;

    public static implicit operator string(PermissionCode code) => code.Value;
}
