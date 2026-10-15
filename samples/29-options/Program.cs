using SupportDesk.Application;
using SupportDesk.Infrastructure;
using SupportDesk.Hosting;
using SupportDesk.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<ITicketStore, InMemoryTicketStore>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOptions<TicketOptions>()
    .Bind(builder.Configuration.GetSection(TicketOptions.SectionName))
    .Validate(o => o.DefaultPageSize >= 1 && o.DefaultPageSize <= o.MaxPageSize,
        "DefaultPageSize must be between 1 and MaxPageSize.")
    .Validate(o => o.MaxPageSize >= 1 && o.MaxPageSize <= 100,
        "MaxPageSize must be between 1 and 100.")
    .ValidateOnStart();
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
