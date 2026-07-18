using FluentValidation;
using FluentValidation.Results;

namespace eHub.Application.Common.Validation;

public static class ValidationExtensions
{
    public static async Task EnsureValidAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }

    public static IReadOnlyDictionary<string, object?> ToProblemDetails(
        this IEnumerable<ValidationFailure> failures)
    {
        var errors = failures
            .GroupBy(failure => string.IsNullOrWhiteSpace(failure.PropertyName) ? "_" : failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => (object?)group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return new Dictionary<string, object?> { ["errors"] = errors };
    }
}
