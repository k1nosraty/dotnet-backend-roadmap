using SupportDesk.Application;
namespace SupportDesk.Features;

internal static class QueryEndpoints
{
    public static void MapTicketQueries(this WebApplication app)
    {
        app.MapGet("/api/tickets", async (string? q, string? status, int? page, int? pageSize,
            ListTickets handler, CancellationToken ct) =>
        {
            var result = await handler.ExecuteAsync(new(q, status, page ?? 1, pageSize ?? 20), ct);
            return result is null ? Results.Problem(statusCode: 400, title: "Invalid page or filter.")
                : (IResult)Results.Ok(result);
        });
        app.MapGet("/api/tickets/{id:int}", async (int id, GetTicket handler, CancellationToken ct) =>
        {
            if (id <= 0) return Results.Problem(statusCode: 400, title: "Invalid identifier.");
            var result = await handler.ExecuteAsync(id, ct);
            return result is null ? Results.Problem(statusCode: 404, title: "Ticket not found.")
                : (IResult)Results.Ok(result);
        });
    }
}
