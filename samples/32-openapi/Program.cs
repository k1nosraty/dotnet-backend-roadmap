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
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
builder.Services.AddScoped<RequestStamp>();
builder.Services.AddOpenApi();
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
    app.MapGet("/diagnostics/scope", (RequestStamp stamp, TicketService service) =>
        Results.Ok(new { direct = stamp.Id, viaService = service.RequestId }))
        .ExcludeFromDescription();
    app.MapOpenApi();
}
app.Run();
