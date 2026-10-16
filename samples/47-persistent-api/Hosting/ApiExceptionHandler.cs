using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace SupportDesk.Hosting;
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger,
    IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        // Do not log exception messages: SQL diagnostics can contain data or connection details.
        logger.LogError("Request {RequestId} failed with {ErrorType}", context.TraceIdentifier, exception.GetType().Name);
        bool unavailable = exception is Npgsql.NpgsqlException or TimeoutException
            || exception is Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: Npgsql.NpgsqlException };
        int status = unavailable ? 503 : 500;
        context.Response.StatusCode = status;
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status, Title = unavailable ? "Database unavailable" : "Unexpected server error",
                Extensions = { ["requestId"] = context.TraceIdentifier }
            }
        });
        return true;
    }
}
