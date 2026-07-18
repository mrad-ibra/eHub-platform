using eHub.Application.Abstractions.Audit;
using eHub.Application.Common.Behaviors;
using eHub.Application.Common.Context;
using eHub.Application.Configuration;
using eHub.Application.Identity.Abstractions;
using eHub.Application.Identity.Authorization;
using eHub.Application.Identity.Services;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<ProblemDetailsOptions>(configuration.GetSection(ProblemDetailsOptions.SectionName));
        services.Configure<AuthorizationOptions>(configuration.GetSection(AuthorizationOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<SiteOptions>(configuration.GetSection(SiteOptions.SectionName));
        services.Configure<CatalogOptions>(configuration.GetSection(CatalogOptions.SectionName));

        services.AddSingleton<IAuthorizationCatalog, AuthorizationCatalog>();
        services.AddScoped<IAuthSessionFactory, AuthSessionFactory>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<ILoginHistoryRecorder, LoginHistoryRecorder>();
        services.AddScoped<IAuditContext, CurrentUserAuditContext>();
        services.AddScoped<IAuditFieldStamper, AuditFieldStamper>();

        var applicationAssembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(applicationAssembly);
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
