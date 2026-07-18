namespace eHub.Domain.Identity;

/// <summary>
/// Declarative role metadata used for seeding and discovery.
/// </summary>
public sealed record RoleDefinition(
    string Name,
    string? Description = null,
    IReadOnlyList<string>? Permissions = null,
    bool IsSystem = true);
