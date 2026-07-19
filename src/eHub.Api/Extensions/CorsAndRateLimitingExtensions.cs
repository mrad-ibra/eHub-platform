using System.Threading.RateLimiting;
using eHub.Application.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using CorsOptions = eHub.Application.Configuration.CorsOptions;
using RateLimitingOptions = eHub.Application.Configuration.RateLimitingOptions;

namespace eHub.Api.Extensions;

public static class CorsAndRateLimitingExtensions
{
    public static IServiceCollection AddEHubCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));
        var origins = configuration.GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>()?.AllowedOrigins ?? [];

        if (origins.Length == 0)
        {
            return services;
        }

        services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddEHubRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));
        var auth = configuration.GetSection(RateLimitingOptions.SectionName)
            .Get<RateLimitingOptions>()?.Auth ?? new AuthRateLimitOptions();

        var permit = Math.Max(1, auth.PermitLimit);
        var windowSeconds = Math.Max(1, auth.WindowSeconds);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitOptions.PolicyName, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permit,
                        Window = TimeSpan.FromSeconds(windowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        return services;
    }

    public static WebApplication UseEHubCors(this WebApplication app)
    {
        var origins = app.Configuration.GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>()?.AllowedOrigins ?? [];

        if (origins.Length > 0)
        {
            app.UseCors("Frontend");
        }

        return app;
    }
}
