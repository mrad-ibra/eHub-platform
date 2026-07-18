namespace eHub.Domain.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public IReadOnlyDictionary<string, object?>? Details { get; }

    public AppException(
        int statusCode,
        string code,
        string message,
        IReadOnlyDictionary<string, object?>? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Details = details;
    }
}

public sealed class NotFoundException(string message)
    : AppException(StatusCodes.NotFound, "not_found", message);

public sealed class ConflictException(string message)
    : AppException(StatusCodes.Conflict, "conflict", message);

public sealed class ValidationFailedException(
    string message,
    IReadOnlyDictionary<string, object?>? details = null)
    : AppException(StatusCodes.BadRequest, "validation_failed", message, details);

public sealed class ForbiddenAccessException(string message)
    : AppException(StatusCodes.Forbidden, "forbidden", message);

public sealed class AuthenticationFailedException(string message)
    : AppException(StatusCodes.Unauthorized, "authentication_failed", message);

public sealed class ConfigurationException(string message)
    : AppException(StatusCodes.InternalServerError, "configuration_error", message);

file static class StatusCodes
{
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int InternalServerError = 500;
}
