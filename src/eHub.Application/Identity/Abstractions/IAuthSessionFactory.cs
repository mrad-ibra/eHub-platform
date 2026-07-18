using eHub.Application.Identity.Commands.Login;
using eHub.Domain.Identity;

namespace eHub.Application.Identity.Abstractions;

public interface IAuthSessionFactory
{
    Task<AuthSessionResult> CreateAsync(User user, CancellationToken cancellationToken = default);
}
