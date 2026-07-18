using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace eHub.Api.Swagger;

public sealed class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse
                ? "default"
                : responseType.StatusCode.ToString();

            if (!operation.Responses.TryGetValue(responseKey, out var response))
            {
                continue;
            }

            foreach (var contentType in response.Content.Keys
                         .Where(contentType => responseType.ApiResponseFormats
                             .All(format => format.MediaType != contentType))
                         .ToList())
            {
                response.Content.Remove(contentType);
            }
        }

        if (operation.Parameters is null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions
                .First(p => p.Name == parameter.Name);

            parameter.Description ??= description.ModelMetadata?.Description;

            if (parameter.Schema.Default is null && description.DefaultValue is not null)
            {
                parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(
                    System.Text.Json.JsonSerializer.Serialize(description.DefaultValue));
            }

            parameter.Required |= description.IsRequired;
        }
    }
}
