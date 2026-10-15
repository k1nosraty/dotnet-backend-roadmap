using SupportDesk.Application;
using SupportDesk.Infrastructure;
using SupportDesk.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<ITicketStore, InMemoryTicketStore>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
var app = builder.Build();
app.UseMiddleware<RequestIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
if (app.Environment.IsDevelopment())
{
    app.MapGet("/diagnostics/failure", (Func<IResult>)(() =>
        throw new InvalidOperationException("course-internal-sentinel")))
        .ExcludeFromDescription();
}
app.Run();
