using Asp.Versioning.ApiExplorer;
using eHub.Api.Swagger;
using eHub.Localization;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace eHub.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddEHubSwagger(this IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
            options.OperationFilter<SwaggerDefaultValues>();
        });

        return services;
    }

    public static WebApplication UseEHubSwagger(this WebApplication app)
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        var apiTitle = MessageResources.Get(MessageCodes.ApiTitle);

        app.UseSwagger(options =>
        {
            options.RouteTemplate = "openapi/{documentName}.json";
        });

        app.UseSwaggerUI(options =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/openapi/{description.GroupName}.json",
                    $"{apiTitle} {description.GroupName}");
            }

            options.RoutePrefix = "swagger";
            options.DocumentTitle = apiTitle;
            options.DisplayRequestDuration();
        });

        return app;
    }
}
