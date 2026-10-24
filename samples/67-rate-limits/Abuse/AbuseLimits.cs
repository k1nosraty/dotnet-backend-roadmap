using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
namespace SupportDesk.Abuse;
public static class AbuseLimits
{
    public static void AddAbuseLimits(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    context.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                await Results.Problem(statusCode: 429, title: "Request budget exceeded.").ExecuteAsync(context.HttpContext);
            };
            // Do not trust X-Forwarded-For. No forwarded-header processing is enabled in this lab.
            options.AddPolicy("credentials", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
            options.AddPolicy("tickets", context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.Identity?.IsAuthenticated == true && context.User.FindFirstValue(ClaimTypes.NameIdentifier) is { } id
                    ? "user:" + id : "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
                _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 32, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
        });
    }
}
