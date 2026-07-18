using eHub.Application.Common.Time;
using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using eHub.Domain.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eHub.Infrastructure.Identity;

public sealed class AuthSeedHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<AuthOptions> options,
    ILogger<AuthSeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var seed = options.Value.Seed;
        if (!seed.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(seed.Email) || string.IsNullOrWhiteSpace(seed.Password))
        {
            logger.LogWarning("Auth seed is enabled but email/password is missing");
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var email = seed.Email.Trim().ToLowerInvariant();
        var existing = await users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var user = User.SeedAdmin(email, hasher.Hash(seed.Password), seed.FullName, clock.UtcNow);
        await users.AddAsync(user, cancellationToken);
        logger.LogInformation("Seeded auth admin user {Email}", email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
