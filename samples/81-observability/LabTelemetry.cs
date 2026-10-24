using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
namespace SupportDesk;

public sealed class LabTelemetry : IDisposable
{
    public const string Name = "SupportDesk.WorkerLab";
    private readonly ActivitySource source = new(Name);
    private readonly Meter meter = new(Name);
    private readonly Counter<long> jobs;
    public LabTelemetry() => jobs = meter.CreateCounter<long>("worker.jobs", unit: "{job}");
    public void Record(bool accepted)
    {
        var outcome = accepted ? "accepted" : "rejected";
        using var activity = source.StartActivity("worker.process");
        activity?.SetTag("job.outcome", outcome);
        if (!accepted) activity?.SetStatus(ActivityStatusCode.Error, "job_rejected");
        jobs.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
    }
    public void Dispose() { source.Dispose(); meter.Dispose(); }
}
public sealed class StartupState
{
    private int ready;
    public bool Ready => Volatile.Read(ref ready) == 1;
    public void SetReady(bool value) => Volatile.Write(ref ready, value ? 1 : 0);
}
public sealed class StartupCheck(StartupState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(state.Ready ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());
}
public sealed class StartupLifecycle(StartupState state) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // shortcut: this lab has only local initialization; add bounded dependency checks when those dependencies exist.
        state.SetReady(true); return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) { state.SetReady(false); return Task.CompletedTask; }
}
public static class TelemetryRegistration
{
    public static void AddLabTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<LabTelemetry>();
        services.AddSingleton<StartupState>();
        services.AddHostedService<StartupLifecycle>();
        services.AddHealthChecks().AddCheck<StartupCheck>("startup", tags: ["ready"]);
        services.AddOpenTelemetry().ConfigureResource(resource => resource.AddService("worker-lab", serviceVersion: "1.0.0"))
            .WithTracing(tracing => { tracing.SetSampler(new AlwaysOnSampler()).AddSource(LabTelemetry.Name).AddConsoleExporter(); })
            .WithMetrics(metrics => { metrics.AddMeter(LabTelemetry.Name).AddConsoleExporter(); });
    }
}
