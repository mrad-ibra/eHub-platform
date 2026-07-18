namespace eHub.Domain.Identity;

/// <summary>
/// Declarative permission metadata used for seeding and discovery.
/// Runtime authorization resolves against persisted <see cref="Permission"/> rows, not these definitions.
/// </summary>
public sealed record PermissionDefinition(
    string Code,
    string Name,
    string Module,
    string? Description = null);
