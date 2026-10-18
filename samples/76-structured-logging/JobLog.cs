using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace SupportDesk;
public enum DeliveryResult { Delivered, Rejected }
public sealed record Notification(Guid Id, string Email, string Body);
public sealed class JobLog(ILogger<JobLog> logger)
{
    public void Finished(Notification notification, DeliveryResult result)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["JobId"] = notification.Id });
        if (result == DeliveryResult.Delivered)
            logger.LogInformation(new EventId(7701, "DeliveryFinished"), "Delivery finished with {Outcome}", "delivered");
        else
            logger.LogWarning(new EventId(7702, "DeliveryRejected"), "Delivery finished with {Outcome}", "rejected");
        // Email, Body, headers, tokens and exception text are never passed to the logger.
    }
}
public sealed class JsonLineSink(TextWriter writer) : ILogEventSink
{
    private readonly object gate = new();
    private readonly JsonFormatter formatter = new(renderMessage: true);
    public void Emit(LogEvent logEvent)
    {
        lock (gate) { formatter.Format(logEvent, writer); writer.Flush(); }
    }
}
