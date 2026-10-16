using SupportDesk.Application;
using SupportDesk.Persistence;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Hosting;
using SupportDesk.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
string connection = Environment.GetEnvironmentVariable("SUPPORTDESK_PG")
    ?? throw new InvalidOperationException("Set SUPPORTDESK_PG for the dedicated application database.");
builder.Services.AddDbContext<DeskContext>(o => o.UseNpgsql(connection));
builder.Services.AddScoped<ITicketData, EfTicketData>();
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
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
builder.Services.AddScoped<RequestStamp>();
builder.Services.AddOpenApi();
var app = builder.Build();
// Explicit maintenance command; ordinary startup never changes the schema.
if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<DeskContext>();
    if (db.Database.HasPendingModelChanges()) throw new InvalidOperationException("Migration snapshot drift.");
    await db.Database.MigrateAsync();
    Console.WriteLine("PASS: application migrations applied");
    return;
}
app.UseMiddleware<RequestIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (DeskContext db, CancellationToken ct) =>
{
    try
    {
        // An empty table is healthy; a missing schema is not. CanConnect alone is insufficient.
        _ = await db.Tickets.AsNoTracking().Select(t => t.Id).Take(1).ToArrayAsync(ct);
        return Results.Ok(new { status = "ready" });
    }
    catch (Exception ex) when (ex is Npgsql.NpgsqlException || ex is TimeoutException)
    { return Results.Problem(statusCode: 503, title: "Database unavailable"); }
});
if (app.Environment.IsDevelopment())
{
    app.MapGet("/diagnostics/failure", (Func<IResult>)(() =>
        throw new InvalidOperationException("course-internal-sentinel")))
        .ExcludeFromDescription();
    app.MapGet("/diagnostics/scope", (RequestStamp stamp, TicketService service) =>
        Results.Ok(new { direct = stamp.Id, viaService = service.RequestId }))
        .ExcludeFromDescription();
    app.MapOpenApi();
}
app.Run();
