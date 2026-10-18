using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SupportDesk;
using Xunit;

public sealed class LoggingTests
{
    [Theory]
    [InlineData(DeliveryResult.Delivered, "Information", "delivered", 7701)]
    [InlineData(DeliveryResult.Rejected, "Warning", "rejected", 7702)]
    public void Json_has_searchable_fields_and_omits_sensitive_content(DeliveryResult result, string level, string outcome, int eventId)
    {
        using var output = new StringWriter();
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(config => config.MinimumLevel.Information().Enrich.FromLogContext()
            .WriteTo.Sink(new JsonLineSink(output)));
        builder.Services.AddTransient<JobLog>();
        using (var host = builder.Build())
        {
            var log = host.Services.GetRequiredService<JobLog>();
            log.Finished(new Notification(Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "secret-email@example.invalid", "password=DoNotLog\nInjectedEvent"), result);
        }
        var text = output.ToString();
        Assert.DoesNotContain("secret-email", text);
        Assert.DoesNotContain("DoNotLog", text);
        Assert.DoesNotContain("InjectedEvent", text);
        var line = Assert.Single(text.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        using var json = JsonDocument.Parse(line);
        var root = json.RootElement;
        Assert.Equal(level, root.GetProperty("Level").GetString());
        Assert.Equal("Delivery finished with {Outcome}", root.GetProperty("MessageTemplate").GetString());
        var props = root.GetProperty("Properties");
        Assert.Equal(outcome, props.GetProperty("Outcome").GetString());
        Assert.Equal("11111111-1111-1111-1111-111111111111", props.GetProperty("JobId").GetString());
        Assert.Equal(eventId, props.GetProperty("EventId").GetProperty("Id").GetInt32());
        Assert.Equal("SupportDesk.JobLog", props.GetProperty("SourceContext").GetString());
    }
    [Fact]
    public void Scope_does_not_leak_into_later_events()
    {
        using var output = new StringWriter();
        using var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(new JsonLineSink(output)).CreateLogger();
        using var factory = LoggerFactory.Create(b => b.AddSerilog(logger, dispose: false));
        new JobLog(factory.CreateLogger<JobLog>()).Finished(new Notification(Guid.NewGuid(), "secret", "secret"), DeliveryResult.Delivered);
        factory.CreateLogger("After").LogInformation("Outside delivery");
        var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        using var last = JsonDocument.Parse(lines[1]);
        Assert.False(last.RootElement.GetProperty("Properties").TryGetProperty("JobId", out _));
    }
}
