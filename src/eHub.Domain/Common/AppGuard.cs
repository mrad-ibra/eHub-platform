using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Domain.Common;

public static class AppGuard
{
    public static string NotEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.FieldRequired, paramName));
        }

        return value;
    }

    public static Guid NotEmpty(Guid value, string paramName)
    {
        if (value == Guid.Empty)
        {
            throw new ValidationFailedException(ErrorResources.Get(ErrorCodes.FieldRequired, paramName));
        }

        return value;
    }
}
