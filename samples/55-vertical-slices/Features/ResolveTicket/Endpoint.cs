using SupportDesk.Application;
using SupportDesk.Presentation;
namespace SupportDesk.Features;

internal static class ResolveEndpoint
{
    public static void MapResolveTicket(this WebApplication app)
    {
        app.MapPost("/api/tickets/{id:int}/resolve", async (int id, ResolveRequest request, ResolveTicket handler, CancellationToken ct) =>
            OutcomeHttp.ToHttp(await handler.ExecuteAsync(RequestMapping.ToCommand(id, request), ct)));
    }
}
