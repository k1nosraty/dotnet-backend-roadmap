using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using SupportDesk;
var builder=WebApplication.CreateBuilder(args);
var connection=Environment.GetEnvironmentVariable("SUPPORTDESK_JOBS");
if(string.IsNullOrWhiteSpace(connection)) { Console.Error.WriteLine("Set SUPPORTDESK_JOBS for the lab database."); Environment.ExitCode=2; return; }
NpgsqlDataSource data;
try { data=NpgsqlDataSource.Create(connection); }
catch(ArgumentException) { Console.Error.WriteLine("Invalid database configuration."); Environment.ExitCode=2; return; }
builder.Services.AddSingleton<NpgsqlDataSource>(_=>data);
builder.Services.AddScoped<QueueStore>();
builder.Services.AddHostedService<JobWorker>();
builder.Services.AddHealthChecks().AddCheck<DatabaseReady>("database",timeout:TimeSpan.FromSeconds(3));
builder.Logging.AddFilter("Microsoft.AspNetCore",LogLevel.Warning);
var app=builder.Build();
app.MapHealthChecks("/health/live",new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate=_=>false });
app.MapHealthChecks("/health/ready");
var version=typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a=>a.Key=="CourseVersion").Value;
app.MapGet("/version",()=>Results.Ok(new { service="durable-worker",version }));
app.Run();
public partial class Program { }
public sealed class DatabaseReady(NpgsqlDataSource data,IConfiguration config) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken=default)
    {
        if(config.GetValue("Maintenance",false)) return HealthCheckResult.Unhealthy();
        try
        {
            await using var command=data.CreateCommand("SELECT count(*) FROM jobs WHERE false");
            command.CommandTimeout=2;
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch(NpgsqlException) { return HealthCheckResult.Unhealthy(); }
    }
}
