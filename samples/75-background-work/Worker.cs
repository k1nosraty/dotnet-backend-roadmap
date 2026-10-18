using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SupportDesk;
public sealed record Job(int Id);
public sealed class JobQueue
{
    private readonly Channel<Job> channel = Channel.CreateBounded<Job>(new BoundedChannelOptions(2)
    { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
    public ValueTask EnqueueAsync(Job job, CancellationToken ct) => channel.Writer.WriteAsync(job, ct);
    public void Complete() => channel.Writer.TryComplete();
    public IAsyncEnumerable<Job> ReadAllAsync(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}
public interface IJobProcessor { Task ProcessAsync(Job job, CancellationToken ct); }
public sealed class JobJournal
{
    public ConcurrentQueue<(int Job, Guid Scope)> Completed { get; } = new();
    public ConcurrentQueue<Guid> Disposed { get; } = new();
}
public sealed class DemoProcessor(JobJournal journal) : IJobProcessor, IAsyncDisposable
{
    private readonly Guid scopeId = Guid.NewGuid();
    public Task ProcessAsync(Job job, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        journal.Completed.Enqueue((job.Id, scopeId));
        return Task.CompletedTask;
    }
    public ValueTask DisposeAsync() { journal.Disposed.Enqueue(scopeId); return ValueTask.CompletedTask; }
}
public sealed class JobWorker(JobQueue queue, IServiceScopeFactory scopes, ILogger<JobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in queue.ReadAllAsync(stoppingToken))
            {
                await using var scope = scopes.CreateAsyncScope();
                try
                {
                    await scope.ServiceProvider.GetRequiredService<IJobProcessor>().ProcessAsync(job, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (InvalidOperationException)
                {
                    // Expected demo job failure. Never log arbitrary exception text or payloads.
                    logger.LogWarning(new EventId(7601, "JobRejected"), "Job {JobId} rejected", job.Id);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
public static class WorkerRegistration
{
    public static IServiceCollection AddJobLab(this IServiceCollection services)
    {
        services.AddSingleton<JobQueue>();
        services.AddSingleton<JobJournal>();
        services.AddScoped<IJobProcessor, DemoProcessor>();
        services.AddSingleton<JobWorker>();
        services.AddHostedService(p => p.GetRequiredService<JobWorker>());
        return services;
    }
}
