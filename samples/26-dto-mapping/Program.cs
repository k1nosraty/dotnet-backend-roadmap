using SupportDesk.Application;
using SupportDesk.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<ITicketStore, InMemoryTicketStore>();
builder.Services.AddScoped<TicketService>();
var app = builder.Build();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
