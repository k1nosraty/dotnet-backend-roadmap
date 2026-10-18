using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SupportDesk;
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Services.AddJobLab();
using var host = builder.Build();
await host.StartAsync();
var queue = host.Services.GetRequiredService<JobQueue>();
await queue.EnqueueAsync(new Job(1), default);
await queue.EnqueueAsync(new Job(2), default);
queue.Complete(); // Normal completion drains the queue. StopAsync instead cancels remaining work.
await host.Services.GetRequiredService<JobWorker>().ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));
await host.StopAsync();
var journal = host.Services.GetRequiredService<JobJournal>();
Console.WriteLine($"completed={journal.Completed.Count}; disposed={journal.Disposed.Count}");
