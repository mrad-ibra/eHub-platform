using eHub.Domain.Exceptions;
using eHub.Localization;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace eHub.Persistence;

/// <summary>
/// Maps PostgreSQL uniqueness / exclusion failures to domain conflicts.
/// Application-level checks are UX-only; DB constraints are the correctness line.
/// </summary>
public static class PostgresExceptionMapper
{
    public static bool TryMap(Exception exception, out Exception mapped)
    {
        mapped = null!;
        var pg = FindPostgresException(exception);
        if (pg is null)
        {
            return false;
        }

        switch (pg.SqlState)
        {
            case PostgresErrorCodes.UniqueViolation:
                mapped = new ConflictException(MapUnique(pg));
                return true;
            case PostgresErrorCodes.ExclusionViolation:
                mapped = new ConflictException(ErrorResources.Get(ErrorCodes.BookingConflict));
                return true;
            default:
                return false;
        }
    }

    public static void ThrowIfMapped(Exception exception)
    {
        if (TryMap(exception, out var mapped))
        {
            throw mapped;
        }
    }

    private static string MapUnique(PostgresException pg)
    {
        var constraint = pg.ConstraintName ?? string.Empty;
        if (constraint.Contains("idempotency", StringComparison.OrdinalIgnoreCase)
            || constraint.Contains("booking_idempotency", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorResources.Get(ErrorCodes.BookingIdempotencyPayloadMismatch);
        }

        if (constraint.Contains("BookingNumber", StringComparison.OrdinalIgnoreCase)
            || constraint.Contains("booking_number", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorResources.Get(ErrorCodes.BookingConflict);
        }

        return ErrorResources.Get(ErrorCodes.BookingConflict);
    }

    private static PostgresException? FindPostgresException(Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is PostgresException pg)
            {
                return pg;
            }

            exception = exception.InnerException;
        }

        return null;
    }
}
