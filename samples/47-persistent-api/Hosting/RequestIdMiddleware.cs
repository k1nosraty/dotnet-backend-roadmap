namespace SupportDesk.Hosting;
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // A callback survives exception-handler response clearing and runs before headers are sent.
        // The ID is server-generated; do not trust a caller's X-Request-ID.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Request-ID"] = context.TraceIdentifier;
            return Task.CompletedTask;
        });
        await next(context);
    }
}
