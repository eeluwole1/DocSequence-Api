using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DocSequence.Api.Infrastructure;

// SRS §13.6 / §15: database unavailable -> 503, anything else -> 500. Always ProblemDetails,
// never a stack trace, and never a fabricated identifier.
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var databaseUnavailable = IsDatabaseUnavailable(exception);

        if (databaseUnavailable)
            logger.LogError(exception, "Database unavailable");
        else
            logger.LogError(exception, "Unhandled exception");

        var problem = databaseUnavailable
            ? new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service temporarily unavailable",
                Detail = "The database could not be reached. No number was allocated; retry with the same requestKey."
            }
            : new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Unexpected error",
                Detail = "The request failed. No number was allocated."
            };

        context.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static bool IsDatabaseUnavailable(Exception exception)
    {
        for (var ex = exception; ex is not null; ex = ex.InnerException)
        {
            if (ex is TimeoutException)
                return true;

            // Class >= 20 = connection-level failure (includes the LocalDB startup error seen in Step 4);
            // the numbers are common network/timeout/Azure-SQL-unavailable codes.
            if (ex is SqlException sql &&
                (sql.Class >= 20 || sql.Number is -2 or 2 or 53 or 233 or 10053 or 10054 or 10060 or 10061 or 40613))
                return true;
        }
        return false;
    }
}