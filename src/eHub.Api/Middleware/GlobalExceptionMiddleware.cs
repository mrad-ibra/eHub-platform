using System.Net;
using System.Text.Json;
using eHub.Api.ProblemDetails;

namespace eHub.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var factory = context.RequestServices.GetRequiredService<EHubProblemDetailsFactory>();
        var (status, code, message, _) = EHubProblemDetailsFactory.Translate(exception);

        if (status >= (int)HttpStatusCode.InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
        else
        {
            logger.LogWarning("Handled application exception {Code}: {Message}", code, message);
        }

        if (context.Response.HasStarted)
        {
            throw exception;
        }

        var problem = factory.Create(context, exception);

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? status;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions));
    }
}
