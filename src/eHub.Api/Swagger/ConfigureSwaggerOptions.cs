using Asp.Versioning.ApiExplorer;
using eHub.Domain.Resources;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace eHub.Api.Swagger;

public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfo(description));
        }
    }

    private static OpenApiInfo CreateInfo(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = MessageResources.Get(MessageCodes.ApiTitle),
            Version = description.ApiVersion.ToString(),
            Description = MessageResources.Get(MessageCodes.ApiDescription),
            Contact = new OpenApiContact
            {
                Name = "eHub",
                Email = "api@ehub.local"
            }
        };

        if (description.IsDeprecated)
        {
            info.Description += MessageResources.Get(MessageCodes.ApiDeprecatedSuffix);
        }

        return info;
    }
}
