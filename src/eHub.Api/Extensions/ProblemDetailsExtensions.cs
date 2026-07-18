using eHub.Api.ProblemDetails;
using Microsoft.AspNetCore.Mvc;

namespace eHub.Api.Extensions;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddEHubProblemDetails(this IServiceCollection services)
    {
        services.AddSingleton<EHubProblemDetailsFactory>();
        services.AddProblemDetails();

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var factory = context.HttpContext.RequestServices.GetRequiredService<EHubProblemDetailsFactory>();
                var problem = factory.CreateValidation(context.HttpContext, context.ModelState);

                return new BadRequestObjectResult(problem)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        return services;
    }
}
