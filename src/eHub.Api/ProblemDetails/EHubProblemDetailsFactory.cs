using System.Diagnostics;
using eHub.Application.Common.Validation;
using eHub.Domain.Exceptions;
using eHub.Localization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using ProblemDetailsOptions = eHub.Application.Configuration.ProblemDetailsOptions;

namespace eHub.Api.ProblemDetails;

public sealed class EHubProblemDetailsFactory(
    IHostEnvironment environment,
    IOptions<ProblemDetailsOptions> options)
{
    private readonly ProblemDetailsOptions _options = options.Value;

    public Microsoft.AspNetCore.Mvc.ProblemDetails Create(HttpContext httpContext, Exception exception)
    {
        var (status, code, message, details) = Translate(exception);
        return Create(httpContext, status, code, message, details, exception);
    }

    public Microsoft.AspNetCore.Mvc.ProblemDetails Create(
        HttpContext httpContext,
        int status,
        string code,
        string detail,
        IReadOnlyDictionary<string, object?>? extensions = null,
        Exception? exception = null)
    {
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = status,
            Title = ToTitle(code),
            Detail = PreferDevelopmentDetail(status, detail, exception),
            Type = BuildType(code),
            Instance = httpContext.Request.Path.Value
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (environment.IsDevelopment()
            && status >= StatusCodes.Status500InternalServerError
            && exception is not null)
        {
            problem.Extensions["exception"] = exception.GetType().FullName;
            problem.Extensions["stackTrace"] = exception.ToString();
        }

        if (extensions is not null)
        {
            foreach (var pair in extensions)
            {
                problem.Extensions[pair.Key] = pair.Value;
            }
        }

        return problem;
    }

    public ValidationProblemDetails CreateValidation(
        HttpContext httpContext,
        ModelStateDictionary modelState)
    {
        var problem = new ValidationProblemDetails(modelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ToTitle("validation_failed"),
            Detail = ErrorResources.Get(ErrorCodes.ValidationFailed),
            Type = BuildType("validation_failed"),
            Instance = httpContext.Request.Path.Value
        };

        problem.Extensions["code"] = "validation_failed";
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return problem;
    }

    public static (int Status, string Code, string Message, IReadOnlyDictionary<string, object?>? Details) Translate(
        Exception exception)
        => exception switch
        {
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "validation_failed",
                ErrorResources.Get(ErrorCodes.ValidationFailed),
                validation.Errors.ToProblemDetails()),
            AppException app => (app.StatusCode, app.Code, app.Message, app.Details),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "unauthorized",
                ErrorResources.Get(ErrorCodes.Unauthorized),
                null),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "not_found",
                string.IsNullOrWhiteSpace(exception.Message)
                    ? ErrorResources.Get(ErrorCodes.NotFound)
                    : exception.Message,
                null),
            ArgumentException arg => (
                StatusCodes.Status400BadRequest,
                "bad_request",
                string.IsNullOrWhiteSpace(arg.Message)
                    ? ErrorResources.Get(ErrorCodes.BadRequest)
                    : arg.Message,
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "internal_error",
                ErrorResources.Get(ErrorCodes.InternalError),
                null)
        };

    private string BuildType(string code)
        => $"{_options.ErrorBaseUrl.TrimEnd('/')}/{code}";

    private string PreferDevelopmentDetail(int status, string detail, Exception? exception)
    {
        if (environment.IsDevelopment()
            && status >= StatusCodes.Status500InternalServerError
            && exception is not null)
        {
            return exception.Message;
        }

        return detail;
    }

    private static string ToTitle(string code)
        => string.Join(' ', code.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(static part => char.ToUpperInvariant(part[0]) + part[1..]));
}
