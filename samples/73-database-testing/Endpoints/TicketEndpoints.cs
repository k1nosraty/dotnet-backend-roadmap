using System.Security.Claims;
using System.Text.Json.Serialization;
using SupportDesk.Application;
namespace SupportDesk.Endpoints;
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TitleInput(string? Title);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ResolveInput(string? Resolution);
public static class TicketEndpoints
{
    public static void MapTickets(this WebApplication app)
    {
        var group = app.MapGroup("/api/tickets").RequireAuthorization();
        group.MapPost("", async (TitleInput input, ClaimsPrincipal user, Tickets tickets, CancellationToken ct) =>
        {
            var result = await tickets.CreateAsync(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", input.Title, ct);
            return result.Outcome == Outcome.Ok ? Results.Created("/api/tickets/" + result.Ticket!.Id, result.Ticket) : Map(result);
        });
        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, Tickets tickets, CancellationToken ct) =>
            Map(await tickets.GetAsync(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", id, ct)));
        group.MapPost("/{id:guid}/start", async (Guid id, ClaimsPrincipal user, Tickets tickets, CancellationToken ct) =>
            Map(await tickets.ChangeAsync(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", id, false, null, ct)));
        group.MapPost("/{id:guid}/resolve", async (Guid id, ResolveInput input, ClaimsPrincipal user, Tickets tickets, CancellationToken ct) =>
            Map(await tickets.ChangeAsync(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "", id, true, input.Resolution, ct)));
    }
    private static IResult Map(Result result) => result.Outcome switch
    {
        Outcome.Ok => Results.Ok(result.Ticket),
        Outcome.Invalid => Results.BadRequest(),
        Outcome.Missing => Results.NotFound(),
        _ => Results.Conflict()
    };
}
