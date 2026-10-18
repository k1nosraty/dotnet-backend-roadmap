using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
namespace SupportDesk.Review;

public static class SecurityReview
{
    private static readonly HashSet<string> PublicRoutes = new(StringComparer.Ordinal)
    { "/health", "/health/ready", "/auth/csrf", "/auth/login", "/auth/refresh" };
    public static void Run(WebApplication app)
    {
        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>().ToArray();
        Validate(routes);
        // Mutation check: a newly exposed public API must make this review fail.
        var bad = new RouteEndpointBuilder(_ => Task.CompletedTask, RoutePatternFactory.Parse("/api/accidental-public"), 0);
        bad.Metadata.Add(new AllowAnonymousAttribute());
        bool rejected = false;
        try { Validate(routes.Append((RouteEndpoint)bad.Build())); }
        catch (InvalidOperationException) { rejected = true; }
        if (!rejected) throw new InvalidOperationException("Review accepted an unexpected anonymous route.");
        Console.WriteLine("PASS: actual route metadata and deliberate public-route regression rejection");
        Console.WriteLine(JsonSerializer.Serialize(routes.Select(e => new
        {
            route = e.RoutePattern.RawText,
            methods = e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods,
            anonymous = e.Metadata.GetMetadata<IAllowAnonymous>() is not null,
            policies = e.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(a => a.Policy ?? "default"),
            rateLimit = e.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
        })));
    }
    private static void Validate(IEnumerable<RouteEndpoint> source)
    {
        var routes = source.ToArray();
        var publicRoutes = routes.Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(e => e.RoutePattern.RawText ?? "").ToHashSet(StringComparer.Ordinal);
        if (!publicRoutes.SetEquals(PublicRoutes)) throw new InvalidOperationException("Anonymous route inventory changed.");
        foreach (var route in routes.Where(e => (e.RoutePattern.RawText ?? "").StartsWith("/api/tickets", StringComparison.Ordinal)))
        {
            if (!route.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(a => a.Policy == "BearerOnly") ||
                route.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName != "tickets")
                throw new InvalidOperationException("Ticket authorization or budget metadata changed.");
        }
        var audit = routes.Single(e => e.RoutePattern.RawText == "/api/staff/audit");
        if (!audit.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(a => a.Policy == "ViewAudit"))
            throw new InvalidOperationException("Audit policy changed.");
        foreach (string path in new[] { "/auth/login", "/auth/refresh" })
            if (routes.Single(e => e.RoutePattern.RawText == path).Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName != "credentials")
                throw new InvalidOperationException("Credential budget changed.");
    }
}
