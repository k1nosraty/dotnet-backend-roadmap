using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace SupportDesk.Hosting;
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger,
    IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled failure for request {RequestId}", context.TraceIdentifier);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = 500, Title = "Unexpected server error",
                Extensions = { ["requestId"] = context.TraceIdentifier }
            }
        });
        return true;
    }
}
