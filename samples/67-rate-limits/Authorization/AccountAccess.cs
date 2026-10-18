using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
namespace SupportDesk.Authorization;

public sealed record Actor(string UserId, string TenantId);
public sealed class AccountAccess(UserManager<IdentityUser> users)
{
    public async Task<Actor?> ReadAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true) return null;
        var user = await users.GetUserAsync(principal);
        if (user is null || !user.EmailConfirmed) return null;
        var tenants = (await users.GetClaimsAsync(user)).Where(c => c.Type == "tenant").ToArray();
        return tenants.Length == 1 && tenants[0].Value.Length > 0 ? new(user.Id, tenants[0].Value) : null;
    }
}
public sealed class ManageTicketsRequirement : IAuthorizationRequirement { }
public sealed class ManageTicketsHandler(UserManager<IdentityUser> users)
    : AuthorizationHandler<ManageTicketsRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ManageTicketsRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;
        var user = await users.GetUserAsync(context.User);
        if (user is null || !user.EmailConfirmed || !await users.IsInRoleAsync(user, "SupportAgent")) return;
        // Read current server-side grants: an older protected token cannot preserve a revoked grant.
        if ((await users.GetClaimsAsync(user)).Any(c => c.Type == "permission" && c.Value == "tickets.manage"))
            context.Succeed(requirement);
    }
}
public static class AccessRegistration
{
    public static void AddTicketAccess(this IServiceCollection services)
    {
        services.AddScoped<AccountAccess>();
        services.AddScoped<IAuthorizationHandler, ManageTicketsHandler>();
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.AddPolicy("ManageTickets", p => p.RequireAuthenticatedUser().AddRequirements(new ManageTicketsRequirement()));
        });
    }
}
