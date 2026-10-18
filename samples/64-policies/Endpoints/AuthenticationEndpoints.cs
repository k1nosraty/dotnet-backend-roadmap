using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
namespace SupportDesk.Endpoints;

public sealed record LoginInput(string? Email, string? Password);
public sealed record RefreshInput(string? RefreshToken);
public static class AuthenticationEndpoints
{
    public static void MapAuthenticationLab(this WebApplication app)
    {
        app.MapGet("/auth/csrf", (HttpContext context, IAntiforgery antiforgery) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { requestToken = antiforgery.GetAndStoreTokens(context).RequestToken });
        }).AllowAnonymous();
        app.MapPost("/auth/login", async (string? mode, LoginInput input, HttpContext context,
            IAntiforgery antiforgery, SignInManager<IdentityUser> signIn) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            mode ??= "bearer";
            if (mode is not ("cookie" or "bearer")) return Results.Problem(statusCode: 400, title: "Unknown login mode.");
            if (mode == "cookie" && !await ValidCsrf(context, antiforgery)) return Results.Problem(statusCode: 400, title: "Invalid CSRF token.");
            if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > 254 || input.Password is null || input.Password.Length > 1024)
                return Results.Unauthorized();
            signIn.AuthenticationScheme = mode == "cookie" ? IdentityConstants.ApplicationScheme : IdentityConstants.BearerScheme;
            var result = await signIn.PasswordSignInAsync(input.Email, input.Password, isPersistent: false, lockoutOnFailure: true);
            if (!result.Succeeded) return Results.Unauthorized();
            // Identity's handler already issued the cookie or wrote its protected-token response.
            return Results.Empty;
        }).AllowAnonymous();
        app.MapPost("/auth/refresh", async (RefreshInput input, IOptionsMonitor<BearerTokenOptions> options,
            TimeProvider clock, SignInManager<IdentityUser> signIn) =>
        {
            if (string.IsNullOrEmpty(input.RefreshToken) || input.RefreshToken.Length > 16384) return Results.Unauthorized();
            var ticket = options.Get(IdentityConstants.BearerScheme).RefreshTokenProtector.Unprotect(input.RefreshToken);
            if (ticket?.Properties.ExpiresUtc is not { } expiry || clock.GetUtcNow() >= expiry ||
                await signIn.ValidateSecurityStampAsync(ticket.Principal) is not IdentityUser user)
                return Results.Unauthorized();
            return Results.SignIn(await signIn.CreateUserPrincipalAsync(user), authenticationScheme: IdentityConstants.BearerScheme);
        }).AllowAnonymous();
        app.MapPost("/auth/logout", async (HttpContext context, IAntiforgery antiforgery, SignInManager<IdentityUser> signIn) =>
        {
            if (!await ValidCsrf(context, antiforgery)) return Results.Problem(statusCode: 400, title: "Invalid CSRF token.");
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization("CookieOnly");
        app.MapGet("/api/me", (HttpContext context, UserManager<IdentityUser> users) =>
            Results.Ok(new { userId = users.GetUserId(context.User) })).RequireAuthorization();
        app.MapPost("/api/cookie-action", async (HttpContext context, IAntiforgery antiforgery) =>
            await ValidCsrf(context, antiforgery) ? Results.NoContent() : (IResult)Results.Problem(statusCode: 400, title: "Invalid CSRF token."))
            .RequireAuthorization("CookieOnly");
        app.MapPost("/api/bearer-action", () => Results.NoContent()).RequireAuthorization("BearerOnly");
    }
    private static async Task<bool> ValidCsrf(HttpContext context, IAntiforgery antiforgery)
    {
        try { await antiforgery.ValidateRequestAsync(context); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
