using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Authorization;
using SupportDesk.Persistence;
namespace SupportDesk.Audit;
public sealed class AuditReadRequirement : IAuthorizationRequirement { }
public sealed class AuditReadHandler(UserManager<IdentityUser> users) : AuthorizationHandler<AuditReadRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AuditReadRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true) return;
        var user = await users.GetUserAsync(context.User);
        if (user is null || !user.EmailConfirmed || !await users.IsInRoleAsync(user, "SupportAgent")) return;
        if ((await users.GetClaimsAsync(user)).Any(c => c.Type == "permission" && c.Value == "audit.read"))
            context.Succeed(requirement);
    }
}
public sealed record AuditView(Guid Id, DateTimeOffset OccurredAt, string ActorId, string Kind, Guid TargetId);
public static class AuditEndpoints
{
    public static void MapAudit(this WebApplication app)
    {
        app.MapGet("/api/staff/audit", async (HttpContext context, AccountAccess accounts, AccountContext db, CancellationToken ct) =>
        {
            var actor = await accounts.ReadAsync(context.User);
            if (actor is null) return (IResult)Results.Forbid();
            var rows = await db.AuditEntries.AsNoTracking().Where(a => a.TenantId == actor.TenantId)
                .OrderByDescending(a => a.OccurredAt).ThenBy(a => a.Id).Take(50).ToArrayAsync(ct);
            return Results.Ok(rows.Select(a => new AuditView(a.Id, a.OccurredAt, a.ActorId, a.Kind.ToString(), a.TargetId)));
        }).RequireAuthorization("ViewAudit").RequireRateLimiting("tickets");
    }
}
