using Microsoft.AspNetCore.Mvc;
using SupportDesk.Contracts;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TicketStore>();
var app = builder.Build();
app.MapGet("/", () => Results.Ok(new { service = "SupportDesk routing demo" }));
app.MapGet("/tickets", ([FromQuery(Name = "q")] string? query,
    [FromQuery] int? limit, [FromServices] TicketStore store) =>
{
    int take = limit ?? 20;
    if (take < 1 || take > 50) return Results.BadRequest(new { error = "Limit must be 1-50." });
    if (query is not null && query.Length > 120) return Results.BadRequest(new { error = "Query is too long." });
    var found = store.All().Where(t => string.IsNullOrWhiteSpace(query) || t.Title.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
        .Take(take).ToArray();
    return Results.Ok(found);
});
app.MapGet("/tickets/{id:int}", ([FromRoute] int id, [FromServices] TicketStore store) => Find(id, store));
// Deliberately without :int to observe binding failure (400) versus no route match (404).
app.MapGet("/lookup/{id}", ([FromRoute] int id, [FromServices] TicketStore store) => Find(id, store));
app.MapGet("/client", ([FromHeader(Name = "X-Client")] string? client) =>
{
    if (client is not null && client.Length > 50) return Results.BadRequest(new { error = "Client label is too long." });
    return Results.Ok(new { client = client ?? "unknown" });
});
app.MapPost("/tickets", ([FromBody] CreateTicketRequest request, [FromServices] TicketStore store) =>
{
    string? title = request.Title?.Trim();
    if (title is null || title.Length < 3 || title.Length > 120)
        return Results.BadRequest(new { error = "Title must contain 3-120 characters." });
    TicketResponse ticket = store.Create(TicketTitle.Create(title));
    return Results.Created($"/tickets/{ticket.Id}", ticket);
});
app.Run();

static IResult Find(int id, TicketStore store)
{
    if (id <= 0) return Results.BadRequest(new { error = "ID must be positive." });
    TicketResponse? ticket = store.Find(id);
    return ticket is null ? Results.NotFound() : Results.Ok(ticket);
}
