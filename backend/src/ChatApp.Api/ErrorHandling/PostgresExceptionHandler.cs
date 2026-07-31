using Microsoft.AspNetCore.Diagnostics;
using Npgsql;

namespace ChatApp.Api.ErrorHandling;

/// <summary>
/// Maps a Postgres constraint violation that slipped past Application's own checks (a race, or an
/// uncovered write path) to a proper HTTP status instead of a raw 500: unique-violation → 409,
/// check-violation → 400.
/// </summary>
public sealed class PostgresExceptionHandler : IExceptionHandler
{
    private const string UniqueViolation = "23505";
    private const string CheckViolation = "23514";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken = default)
    {
        if (exception is not PostgresException postgresException)
        {
            return false;
        }

        var statusCode = postgresException.SqlState switch
        {
            UniqueViolation => StatusCodes.Status409Conflict,
            CheckViolation => StatusCodes.Status400BadRequest,
            _ => (int?)null
        };

        if (statusCode is not { } resolvedStatusCode)
        {
            return false;
        }

        httpContext.Response.StatusCode = resolvedStatusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new { code = postgresException.SqlState, message = postgresException.MessageText },
            cancellationToken);

        return true;
    }
}
