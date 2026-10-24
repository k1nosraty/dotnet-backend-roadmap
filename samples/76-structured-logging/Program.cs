using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SupportDesk;
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Services.AddSerilog(configuration => configuration
    .MinimumLevel.Information().Enrich.FromLogContext().WriteTo.Sink(new JsonLineSink(Console.Out)));
builder.Services.AddTransient<JobLog>();
using var host = builder.Build();
var notification = new Notification(Guid.Parse("11111111-1111-1111-1111-111111111111"), "private@example.invalid", "private body");
host.Services.GetRequiredService<JobLog>().Finished(notification, DeliveryResult.Delivered);
// Host disposal flushes/disposes its Serilog provider; do not create a second static logger.
