using System.Text;
using eHub.Application.Configuration;
using eHub.Application.Identity.Authorization;
using eHub.Domain.Exceptions;
using eHub.Localization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace eHub.Api.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddEHubAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var auth = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? throw new ConfigurationException(ErrorResources.Get(ErrorCodes.JwtConfigMissing));

        if (string.IsNullOrWhiteSpace(auth.Jwt.Key) || auth.Jwt.Key.Length < 32)
        {
            throw new ConfigurationException(ErrorResources.Get(ErrorCodes.JwtConfigMissing));
        }

        if (auth.Jwt.Key.StartsWith("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConfigurationException(ErrorResources.Get(ErrorCodes.JwtSecretPlaceholder));
        }

        services.AddHttpContextAccessor();
        services.AddScoped<Application.Common.Context.IClientContext, Common.HttpClientContext>();
        services.AddScoped<Application.Common.Context.ICurrentUser, Common.HttpCurrentUser>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = auth.Jwt.Issuer,
                    ValidAudience = auth.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.Jwt.Key)),
                    ClockSkew = TimeSpan.FromMinutes(auth.Jwt.ClockSkewMinutes)
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthPolicies.BookingsCreate,
                policy => policy.RequireClaim(AuthPolicies.PermissionClaimType, AuthPolicies.BookingsCreate));
            options.AddPolicy(
                AuthPolicies.BookingsRead,
                policy => policy.RequireClaim(AuthPolicies.PermissionClaimType, AuthPolicies.BookingsRead));
            options.AddPolicy(
                AuthPolicies.BookingsManage,
                policy => policy.RequireClaim(AuthPolicies.PermissionClaimType, AuthPolicies.BookingsManage));
            options.AddPolicy(
                AuthPolicies.PaymentsCreate,
                policy => policy.RequireClaim(AuthPolicies.PermissionClaimType, AuthPolicies.PaymentsCreate));
            options.AddPolicy(
                AuthPolicies.PaymentsRead,
                policy => policy.RequireClaim(AuthPolicies.PermissionClaimType, AuthPolicies.PaymentsRead));
            options.AddPolicy(
                AuthPolicies.PaymentsCancel,
                policy => policy.RequireClaim(AuthPolicies.PermissionClaimType, AuthPolicies.PaymentsCancel));
            options.AddPolicy(
                AuthPolicies.PaymentsRefund,
                policy => policy.RequireClaim(AuthPolicies.PermissionClaimType, AuthPolicies.PaymentsRefund));
            options.AddPolicy(
                AuthPolicies.PaymentsRefundRead,
                policy => policy.RequireClaim(AuthPolicies.PermissionClaimType, AuthPolicies.PaymentsRefundRead));
        });

        services.ConfigureSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "JWT Authorization header using the Bearer scheme.",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
