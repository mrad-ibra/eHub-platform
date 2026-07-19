using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eHub.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddEHubHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var builder = services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        var postgres = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(postgres))
        {
            builder.AddNpgSql(
                postgres,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]);
        }

        var redis = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            builder.AddRedis(
                redis,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]);
        }

        return services;
    }

    public static WebApplication MapEHubHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}
