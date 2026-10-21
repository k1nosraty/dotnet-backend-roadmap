using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SupportDesk;
using Xunit;

public sealed class TelemetryTests
{
    [Fact]
    public async Task Readiness_failure_removes_work_without_making_liveness_fail()
    {
        using var factory = new LabFactory(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        factory.Services.GetRequiredService<StartupState>().SetReady(false);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        using var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal("Unhealthy", await ready.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsync("/work/accepted", null)).StatusCode);
        Assert.Empty(factory.Spans.Items);
    }
    [Theory]
    [InlineData("accepted", 200, ActivityStatusCode.Unset)]
    [InlineData("rejected", 409, ActivityStatusCode.Error)]
    public async Task Actual_sdk_exports_a_span_and_counter_with_only_bounded_outcome(string outcome, int status, ActivityStatusCode spanStatus)
    {
        using var factory = new LabFactory(); using var client = factory.CreateClient();
        using var response = await client.PostAsync($"/work/{outcome}?email=do-not-export@example.invalid", new StringContent("secret body"));
        Assert.Equal(status, (int)response.StatusCode);
        Assert.True(factory.Services.GetRequiredService<TracerProvider>().ForceFlush());
        var span = Assert.Single(factory.Spans.Items);
        Assert.Equal("worker.process", span.OperationName);
        Assert.Equal(spanStatus, span.Status);
        Assert.Equal(outcome, Assert.Single(span.TagObjects).Value);
        Assert.DoesNotContain("secret", string.Join(',', span.TagObjects));
        Assert.DoesNotContain("do-not-export", string.Join(',', span.TagObjects));
        Assert.True(factory.Services.GetRequiredService<MeterProvider>().ForceFlush());
        var point = Assert.Single(factory.Counts.Items);
        Assert.Equal(1, point.Value);
        Assert.Equal(outcome, Assert.Single(point.Tags).Value);
        Assert.Equal("outcome", Assert.Single(point.Tags).Key);
    }
    [Fact]
    public async Task Invalid_outcome_does_not_create_an_unbounded_metric_label()
    {
        using var factory = new LabFactory(); using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/work/user-input", null)).StatusCode);
        factory.Services.GetRequiredService<MeterProvider>().ForceFlush();
        Assert.Empty(factory.Spans.Items); Assert.Empty(factory.Counts.Items);
    }
    private sealed class LabFactory : WebApplicationFactory<Program>
    {
        public SpanExport Spans { get; } = new();
        public CountExport Counts { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services => services.AddOpenTelemetry()
                .WithTracing(tracing => tracing.AddProcessor(new SimpleActivityExportProcessor(Spans)))
                .WithMetrics(metrics => metrics.AddReader(new PeriodicExportingMetricReader(Counts, exportIntervalMilliseconds: Timeout.Infinite))));
        }
    }
    private sealed class SpanExport : BaseExporter<Activity>
    {
        public List<Activity> Items { get; } = new();
        public override ExportResult Export(in Batch<Activity> batch)
        { foreach (var activity in batch) Items.Add(activity); return ExportResult.Success; }
    }
    private sealed record CountPoint(long Value, KeyValuePair<string, object?>[] Tags);
    private sealed class CountExport : BaseExporter<Metric>
    {
        public List<CountPoint> Items { get; } = new();
        public override ExportResult Export(in Batch<Metric> batch)
        {
            foreach (var metric in batch)
                if (metric.Name == "worker.jobs")
                    foreach (ref readonly var point in metric.GetMetricPoints())
                    {
                        var tags = new List<KeyValuePair<string, object?>>();
                        foreach (var tag in point.Tags) tags.Add(tag);
                        Items.Add(new(point.GetSumLong(), tags.ToArray()));
                    }
            return ExportResult.Success;
        }
    }
}
