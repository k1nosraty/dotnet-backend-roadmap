using SupportDesk.Application;
namespace SupportDesk.Features;

public sealed record CreateRequest(string? Title);
public sealed record NoteRequest(string? Text);
public sealed record StatusRequest(string? Status);
internal static class ManageEndpoints
{
    public static void MapManageTickets(this WebApplication app)
    {
        app.MapPost("/api/tickets", async (CreateRequest request, CreateTicket handler, CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new(request.Title), ct);
            return result.Code == OperationCode.Success ? Results.Created($"/api/tickets/{result.Ticket!.Id}", result.Ticket)
                : OutcomeHttp.ToHttp(result);
        });
        app.MapDelete("/api/tickets/{id:int}", async (int id, DeleteTicket handler, CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(id, ct);
            return result.Code == OperationCode.Success ? Results.NoContent() : OutcomeHttp.ToHttp(result);
        });
        app.MapPost("/api/tickets/{id:int}/notes", async (int id, NoteRequest request, ChangeTicket handler, CancellationToken ct) =>
            OutcomeHttp.ToHttp(await handler.AddNoteAsync(new(id, request.Text), ct)));
        app.MapPut("/api/tickets/{id:int}/status", async (int id, StatusRequest request, ChangeTicket handler, CancellationToken ct) =>
            OutcomeHttp.ToHttp(await handler.StatusAsync(new(id, request.Status), ct)));
    }
}
