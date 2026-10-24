using Npgsql;
namespace SupportDesk;
public sealed class JobWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<JobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                if(!config.GetValue("Maintenance",false))
                {
                    try
                    {
                        await using var scope=scopes.CreateAsyncScope();
                        var queue=scope.ServiceProvider.GetRequiredService<QueueStore>();
                        var lease=await queue.ClaimAsync(stoppingToken);
                        if(lease is not null)
                        {
                            await queue.CompleteAsync(lease,stoppingToken);
                            continue;
                        }
                    }
                    catch(NpgsqlException) { logger.LogWarning(new EventId(8601,"DatabaseUnavailable"),"Worker database unavailable"); }
                }
                await Task.Delay(TimeSpan.FromMilliseconds(250),stoppingToken);
            }
        }
        catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) { }
    }
}
