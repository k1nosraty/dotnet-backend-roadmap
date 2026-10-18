using System.Security.Claims;
namespace SupportDesk.Tickets;
public static class TicketEndpoints
{
    public static void MapOwnedTickets(this WebApplication app)
    {
        // All mutations in this lab are bearer-only. A browser cookie alone cannot authorize them.
        var tickets = app.MapGroup("/api/tickets").RequireAuthorization("BearerOnly");
        tickets.MapGet("", (ClaimsPrincipal user, TicketService service, CancellationToken ct) => service.ListAsync(user, null, ct));
        tickets.MapGet("/search", (string? term, ClaimsPrincipal user, TicketService service, CancellationToken ct) => service.ListAsync(user, term, ct));
        tickets.MapGet("/{id:guid}", (Guid id, ClaimsPrincipal user, TicketService service, CancellationToken ct) => service.ReadAsync(user, id, ct));
        tickets.MapPost("", (TicketInput input, ClaimsPrincipal user, TicketService service, CancellationToken ct) => service.CreateAsync(user, input, ct));
        tickets.MapPost("/{id:guid}/title", (Guid id, TicketInput input, ClaimsPrincipal user, TicketService service, CancellationToken ct) => service.RenameAsync(user, id, input, ct));
    }
}
