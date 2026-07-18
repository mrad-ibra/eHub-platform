using eHub.Application.Common.Context;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Authorization;
using eHub.Application.Identity.Commands.Login;
using eHub.Domain.Identity;
using Microsoft.Extensions.Options;

namespace eHub.Application.Identity.Services;

public sealed class AuthSessionFactory(
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    IJwtTokenService jwtTokenService,
    IRefreshTokenHasher refreshTokenHasher,
    IAuthorizationCatalog authorizationCatalog,
    IClientContext clientContext,
    IClock clock,
    IOptions<AuthOptions> authOptions) : IAuthSessionFactory
{
    public async Task<AuthSessionResult> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var roles = ResolveRoleNames(user, authOptions.Value.AccountKindRoles);
        var permissions = ResolvePermissions(roles);

        var refreshOptions = authOptions.Value.RefreshToken;
        var rawRefreshToken = refreshTokenHasher.GenerateRawToken(refreshOptions.TokenSizeBytes);
        var refreshExpiresAt = now.AddDays(refreshOptions.LifetimeDays);
        var refreshToken = RefreshToken.Issue(
            user.Id,
            refreshTokenHasher.Hash(rawRefreshToken),
            refreshExpiresAt,
            now,
            clientContext.IpAddress,
            clientContext.UserAgent);

        var access = jwtTokenService.CreateAccessToken(
            user.Id,
            user.Email,
            user.AccountKind.ToString(),
            roles,
            permissions,
            refreshToken.Id);

        await refreshTokens.AddAsync(refreshToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthSessionResult(
            user.Id,
            user.Email,
            user.FullName,
            user.AccountKind.ToString(),
            roles,
            access.Token,
            access.ExpiresAtUtc,
            refreshToken.Id,
            rawRefreshToken,
            refreshExpiresAt);
    }

    private IReadOnlyList<string> ResolveRoleNames(User user, AccountKindRoleMapOptions roleMap)
    {
        var preferred = user.AccountKind switch
        {
            AccountKind.Admin => roleMap.Admin,
            AccountKind.Business => roleMap.Business,
            _ => roleMap.Personal
        };

        var role = authorizationCatalog.FindRole(preferred);
        return role is null ? [preferred] : [role.Name];
    }

    private IReadOnlyList<string> ResolvePermissions(IReadOnlyList<string> roleNames)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in roleNames)
        {
            var role = authorizationCatalog.FindRole(name);
            if (role?.Permissions is null)
            {
                continue;
            }

            foreach (var permission in role.Permissions)
            {
                set.Add(permission);
            }
        }

        return set.ToArray();
    }
}
