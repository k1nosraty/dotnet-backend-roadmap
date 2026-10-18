using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SupportDesk;
using Xunit;

public sealed class WorkerTests
{
    private static IHost Build(Action<IServiceCollection>? configure = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddJobLab();
        configure?.Invoke(builder.Services);
        return builder.Build();
    }
    [Fact]
    public async Task Completion_drains_jobs_with_a_new_disposed_scope_per_job()
    {
        using var host = Build();
        var queue = host.Services.GetRequiredService<JobQueue>();
        await queue.EnqueueAsync(new(1), default); await queue.EnqueueAsync(new(2), default); queue.Complete();
        await host.StartAsync();
        await host.Services.GetRequiredService<JobWorker>().ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));
        await host.StopAsync();
        var journal = host.Services.GetRequiredService<JobJournal>();
        Assert.Equal(new[] { 1, 2 }, journal.Completed.Select(x => x.Job));
        Assert.Equal(2, journal.Completed.Select(x => x.Scope).Distinct().Count());
        Assert.Equal(journal.Completed.Select(x => x.Scope), journal.Disposed);
    }
    [Fact]
    public async Task Shutdown_cancels_active_job_and_disposes_its_scope()
    {
        var signals = new Signals();
        using var host = Build(s => { s.AddSingleton(signals); s.AddScoped<IJobProcessor, WaitingProcessor>(); });
        await host.StartAsync();
        await host.Services.GetRequiredService<JobQueue>().EnqueueAsync(new(1), default);
        await signals.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await host.StopAsync().WaitAsync(TimeSpan.FromSeconds(10));
        await host.Services.GetRequiredService<JobWorker>().ExecuteTask!;
        Assert.True(signals.Cancelled);
        Assert.True(signals.Disposed);
    }
    [Fact]
    public async Task Full_queue_wait_can_be_cancelled_without_adding_a_job()
    {
        var queue = new JobQueue();
        await queue.EnqueueAsync(new(1), default); await queue.EnqueueAsync(new(2), default);
        using var cts = new CancellationTokenSource();
        var third = queue.EnqueueAsync(new(3), cts.Token).AsTask();
        Assert.False(third.IsCompleted);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => third);
        queue.Complete();
        var jobs = new List<int>();
        await foreach (var job in queue.ReadAllAsync(default)) jobs.Add(job.Id);
        Assert.Equal(new[] {1, 2}, jobs);
    }
    [Fact]
    public async Task Expected_rejection_does_not_prevent_next_job()
    {
        using var host = Build(s => s.AddScoped<IJobProcessor, RejectFirst>());
        var queue = host.Services.GetRequiredService<JobQueue>();
        await queue.EnqueueAsync(new(1), default); await queue.EnqueueAsync(new(2), default); queue.Complete();
        await host.StartAsync();
        await host.Services.GetRequiredService<JobWorker>().ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));
        await host.StopAsync();
        Assert.Equal(2, Assert.Single(host.Services.GetRequiredService<JobJournal>().Completed).Job);
    }
    private sealed class Signals
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Cancelled; public bool Disposed;
    }
    private sealed class WaitingProcessor(Signals signals) : IJobProcessor, IAsyncDisposable
    {
        public async Task ProcessAsync(Job job, CancellationToken ct)
        {
            signals.Started.SetResult();
            try { await Task.Delay(Timeout.Infinite, ct); }
            catch (OperationCanceledException) { signals.Cancelled = true; throw; }
        }
        public ValueTask DisposeAsync() { signals.Disposed = true; return ValueTask.CompletedTask; }
    }
    private sealed class RejectFirst(JobJournal journal) : IJobProcessor
    {
        public Task ProcessAsync(Job job, CancellationToken ct)
        {
            if (job.Id == 1) throw new InvalidOperationException("Expected demo rejection");
            journal.Completed.Enqueue((job.Id, Guid.NewGuid())); return Task.CompletedTask;
        }
    }
}
