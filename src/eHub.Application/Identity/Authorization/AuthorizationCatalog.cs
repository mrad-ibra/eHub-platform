using eHub.Domain.Identity;
using eHub.Localization;
using Microsoft.Extensions.Options;

namespace eHub.Application.Identity.Authorization;

/// <summary>
/// Loads permission/role definitions from configuration (single source of truth for seeding).
/// Module providers can contribute additional definitions without editing shared constants.
/// </summary>
public sealed class AuthorizationCatalog : IAuthorizationCatalog
{
    private readonly IReadOnlyDictionary<string, PermissionDefinition> _permissionsByCode;
    private readonly IReadOnlyDictionary<string, RoleDefinition> _rolesByName;

    public AuthorizationCatalog(
        IOptions<Configuration.AuthorizationOptions> options,
        IEnumerable<IPermissionDefinitionProvider> permissionProviders,
        IEnumerable<IRoleDefinitionProvider> roleProviders)
    {
        var permissions = new Dictionary<string, PermissionDefinition>(StringComparer.OrdinalIgnoreCase);
        var roles = new Dictionary<string, RoleDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in options.Value.Permissions)
        {
            AddPermission(permissions, definition);
        }

        foreach (var provider in permissionProviders)
        {
            foreach (var definition in provider.GetDefinitions())
            {
                AddPermission(permissions, definition);
            }
        }

        foreach (var definition in options.Value.Roles)
        {
            AddRole(roles, definition);
        }

        foreach (var provider in roleProviders)
        {
            foreach (var definition in provider.GetDefinitions())
            {
                AddRole(roles, definition);
            }
        }

        ValidateRolePermissionReferences(roles.Values, permissions.Keys);

        _permissionsByCode = permissions;
        _rolesByName = roles;
        Permissions = permissions.Values.OrderBy(p => p.Module).ThenBy(p => p.Code).ToArray();
        Roles = roles.Values.OrderBy(r => r.Name).ToArray();
    }

    public IReadOnlyCollection<PermissionDefinition> Permissions { get; }
    public IReadOnlyCollection<RoleDefinition> Roles { get; }

    public PermissionDefinition? FindPermission(string code)
        => _permissionsByCode.TryGetValue(code, out var definition) ? definition : null;

    public RoleDefinition? FindRole(string name)
        => _rolesByName.TryGetValue(name, out var definition) ? definition : null;

    private static void AddPermission(
        IDictionary<string, PermissionDefinition> target,
        PermissionDefinition definition)
    {
        var code = PermissionCode.Create(definition.Code);
        target[code.Value] = definition with
        {
            Code = code.Value,
            Name = definition.Name.Trim(),
            Module = definition.Module.Trim(),
            Description = string.IsNullOrWhiteSpace(definition.Description)
                ? null
                : definition.Description.Trim()
        };
    }

    private static void AddRole(
        IDictionary<string, RoleDefinition> target,
        RoleDefinition definition)
    {
        var name = definition.Name.Trim();
        target[name] = definition with
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(definition.Description)
                ? null
                : definition.Description.Trim(),
            Permissions = definition.Permissions?
                .Select(code => PermissionCode.Create(code).Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private static void ValidateRolePermissionReferences(
        IEnumerable<RoleDefinition> roles,
        IEnumerable<string> knownPermissionCodes)
    {
        var known = new HashSet<string>(knownPermissionCodes, StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            if (role.Permissions is null)
            {
                continue;
            }

            foreach (var permissionCode in role.Permissions)
            {
                if (!known.Contains(permissionCode))
                {
                    throw new InvalidOperationException(
                        ErrorResources.Get(ErrorCodes.RoleUnknownPermission, role.Name, permissionCode));
                }
            }
        }
    }
}
