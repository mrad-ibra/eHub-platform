using eHub.Application.Abstractions.Email;
using eHub.Application.Assets.Abstractions;
using eHub.Application.Catalog.Abstractions;
using eHub.Application.Common.Persistence;
using eHub.Application.Common.Time;
using eHub.Application.Identity.Abstractions;
using eHub.Infrastructure.Catalog;
using eHub.Infrastructure.Email;
using eHub.Infrastructure.Identity;
using eHub.Infrastructure.Persistence;
using eHub.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;

namespace eHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenHasher, Sha256RefreshTokenHasher>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();
        services.AddSingleton<ILoginHistoryRepository, InMemoryLoginHistoryRepository>();
        services.AddSingleton<ICatalogStore, InMemoryCatalogStore>();
        services.AddSingleton<IAssetRepository, InMemoryAssetRepository>();
        services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
        services.AddHostedService<AuthSeedHostedService>();
        services.AddHostedService<CatalogSeedHostedService>();

        return services;
    }
}
