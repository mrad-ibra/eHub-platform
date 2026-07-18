using eHub.Api.Middleware;

namespace eHub.Api.Extensions;

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
