using SupportDesk.Application;
namespace SupportDesk.Features;

internal static class CloseEndpoint
{
    public static void MapCloseTicket(this WebApplication app)
    {
        app.MapPost("/api/tickets/{id:int}/close", async (int id, CloseTicket handler, CancellationToken ct) =>
            OutcomeHttp.ToHttp(await handler.ExecuteAsync(id, ct)));
    }
}
