using SupportDesk.Application;
using SupportDesk.Presentation;
namespace SupportDesk.Features;

internal static class RenameEndpoint
{
    public static void MapRenameTicket(this WebApplication app)
    {
        app.MapPut("/api/tickets/{id:int}/title", async (int id, RenameRequest request, RenameTicket handler, CancellationToken ct) =>
            OutcomeHttp.ToHttp(await handler.ExecuteAsync(RequestMapping.ToCommand(id, request), ct)));
    }
}
