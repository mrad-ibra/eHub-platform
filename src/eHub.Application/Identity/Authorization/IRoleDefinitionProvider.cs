using eHub.Domain.Identity;

namespace eHub.Application.Identity.Authorization;

public interface IRoleDefinitionProvider
{
    IReadOnlyCollection<RoleDefinition> GetDefinitions();
}
