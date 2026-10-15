using SupportDesk.Contracts;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TicketStore>();
var app = builder.Build();
app.MapGet("/", () => Results.Ok(new { service = "SupportDesk HTTP demo" }));
app.MapGet("/tickets", (TicketStore store) => Results.Ok(store.All()));
app.MapGet("/tickets/{id:int}", (int id, TicketStore store) =>
{
    if (id <= 0) return Results.BadRequest(new { error = "ID must be positive." });
    TicketResponse? ticket = store.Find(id);
    return ticket is null ? Results.NotFound() : Results.Ok(ticket);
});
app.MapPost("/tickets", (CreateTicketRequest request, TicketStore store) =>
{
    string? raw = request.Title?.Trim();
    if (raw is null || raw.Length < 3 || raw.Length > 120)
        return Results.BadRequest(new { error = "Title must contain 3-120 characters." });
    TicketResponse ticket = store.Create(TicketTitle.Create(raw));
    return Results.Created($"/tickets/{ticket.Id}", ticket);
});
app.MapDelete("/tickets/{id:int}", (int id, TicketStore store) =>
{
    if (id <= 0) return Results.BadRequest(new { error = "ID must be positive." });
    return store.Delete(id) ? Results.NoContent() : Results.NotFound();
});
app.Run();
