namespace SupportDesk.Hosting;
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Server-generated; deliberately do not trust a caller's X-Request-ID.
        context.Response.Headers["X-Request-ID"] = context.TraceIdentifier;
        await next(context);
    }
}
