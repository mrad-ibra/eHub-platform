using eHub.Domain.Identity;

namespace eHub.Application.Identity.Authorization;

public interface IPermissionDefinitionProvider
{
    string Module { get; }

    IReadOnlyCollection<PermissionDefinition> GetDefinitions();
}
