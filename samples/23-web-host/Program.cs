using SupportDesk.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<StartupStamp>();
var app = builder.Build();

app.Use(async (context, next) =>
{
    // Set headers before response output starts.
    context.Response.Headers["X-Course"] = "supportdesk";
    await next(context);
});
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/info", (IHostEnvironment environment, StartupStamp stamp) => Results.Ok(new
{
    environment = environment.EnvironmentName,
    instanceId = stamp.InstanceId
}));
app.MapGet("/greeting", (IConfiguration configuration) => Results.Ok(new
{
    message = configuration["Course:Greeting"] ?? "Hello"
}));
app.Run();
