using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using SupportDesk;
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Services.AddLabTelemetry();
var app = builder.Build();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });
app.MapPost("/work/{outcome}", (string outcome, StartupState state, LabTelemetry telemetry) =>
{
    if (!state.Ready) return Results.StatusCode(503);
    if (outcome is not ("accepted" or "rejected")) return Results.BadRequest();
    telemetry.Record(outcome == "accepted");
    return outcome == "accepted" ? Results.Ok(new { outcome }) : Results.Conflict(new { outcome });
});
app.Run();
public partial class Program { }
