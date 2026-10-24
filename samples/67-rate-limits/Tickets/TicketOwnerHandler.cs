using Microsoft.AspNetCore.Authorization;
using SupportDesk.Authorization;
namespace SupportDesk.Tickets;
public sealed record TicketOperation(string Name) : IAuthorizationRequirement;
public static class TicketOperations
{
    public static readonly TicketOperation Read = new("Read");
    public static readonly TicketOperation Rename = new("Rename");
}
public sealed class TicketOwnerHandler(AccountAccess accounts) : AuthorizationHandler<TicketOperation, OwnedTicket>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TicketOperation requirement, OwnedTicket ticket)
    {
        if (requirement != TicketOperations.Read && requirement != TicketOperations.Rename) return;
        var actor = await accounts.ReadAsync(context.User);
        if (actor is not null && actor.UserId == ticket.OwnerId && actor.TenantId == ticket.TenantId)
            context.Succeed(requirement);
    }
}
