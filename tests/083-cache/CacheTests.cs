using StackExchange.Redis;
using SupportDesk;
using Xunit;
public sealed class CacheTests
{
    private static readonly Guid Tenant = Guid.NewGuid(), Id = Guid.NewGuid();
    private static Task<Summary> Project(Ticket ticket, CancellationToken ct) => Task.FromResult(new Summary(ticket.Version,ticket.Title));
    [Fact]
    public async Task Repeat_read_uses_projection_and_update_changes_version()
    {
        var source = new TicketSource(); source.Write(Tenant,Id,"old"); var builds=0;
        var service = new CachedSummary(source,new MemoryCache(TimeProvider.System),(t,ct) => { builds++; return Project(t,ct); });
        Assert.Equal("old",(await service.ReadAsync(Tenant,Id))!.Label);
        await service.ReadAsync(Tenant,Id); Assert.Equal(1,builds);
        await service.UpdateAsync(Tenant,Id,"new");
        Assert.Equal("new",(await service.ReadAsync(Tenant,Id))!.Label); Assert.Equal(2,builds);
    }
    [Fact]
    public async Task Late_old_projection_cannot_poison_a_new_version()
    {
        var source = new TicketSource(); source.Write(Tenant,Id,"old");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new CachedSummary(source,new MemoryCache(TimeProvider.System),async (ticket,ct) =>
        { if(ticket.Version==1) { entered.SetResult(); await release.Task.WaitAsync(ct); } return new(ticket.Version,ticket.Title); });
        var old = service.ReadAsync(Tenant,Id); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await service.UpdateAsync(Tenant,Id,"new");
        release.SetResult(); Assert.Equal("old",(await old)!.Label);
        Assert.Equal("new",(await service.ReadAsync(Tenant,Id))!.Label);
    }
    [Fact]
    public async Task Keys_separate_tenants_and_missing_owner_is_not_cached()
    {
        var source = new TicketSource(); var other=Guid.NewGuid(); source.Write(Tenant,Id,"one"); source.Write(other,Id,"two");
        var service=new CachedSummary(source,new MemoryCache(TimeProvider.System),Project);
        Assert.Equal("one",(await service.ReadAsync(Tenant,Id))!.Label);
        Assert.Equal("two",(await service.ReadAsync(other,Id))!.Label);
        Assert.Null(await service.ReadAsync(Guid.NewGuid(),Id));
    }
    [Fact]
    public async Task Expiration_uses_clock_without_sleep()
    {
        var clock=new Clock(); var cache=new MemoryCache(clock);
        await cache.PutAsync("k","v",TimeSpan.FromSeconds(30),default); Assert.Equal("v",await cache.GetAsync("k",default));
        clock.Now += TimeSpan.FromSeconds(31); Assert.Null(await cache.GetAsync("k",default));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cache_outage_falls_back_but_projection_failure_is_not_hidden(bool timeout)
    {
        var source=new TicketSource(); source.Write(Tenant,Id,"truth");
        var service=new CachedSummary(source,new BrokenCache(timeout),Project);
        Assert.Equal("truth",(await service.ReadAsync(Tenant,Id))!.Label);
        await service.UpdateAsync(Tenant,Id,"committed"); Assert.Equal("committed",source.Read(Tenant,Id)!.Title);
        var failing=new CachedSummary(source,new BrokenCache(timeout),(_,_)=>throw new InvalidOperationException("source failure"));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>failing.ReadAsync(Tenant,Id));
    }
    [Fact]
    public async Task Malformed_json_is_a_miss_and_cancelled_call_does_not_project()
    {
        var source=new TicketSource(); var ticket=source.Write(Tenant,Id,"truth"); var cache=new MemoryCache(TimeProvider.System);
        await cache.PutAsync(CachedSummary.Key(ticket),"{broken",TimeSpan.FromSeconds(30),default);
        var service=new CachedSummary(source,cache,Project); Assert.Equal("truth",(await service.ReadAsync(Tenant,Id))!.Label);
        using var cancelled=new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>service.ReadAsync(Tenant,Id,cancelled.Token));
    }
    private sealed class Clock : TimeProvider { public DateTimeOffset Now=DateTimeOffset.UnixEpoch; public override DateTimeOffset GetUtcNow()=>Now; }
    private sealed class BrokenCache(bool timeout) : ICache
    {
        private Exception Failure()=>timeout ? new RedisTimeoutException("cache timeout",CommandStatus.Unknown)
            : new RedisConnectionException(ConnectionFailureType.UnableToConnect,"cache unavailable");
        public Task<string?> GetAsync(string key,CancellationToken ct)=>throw Failure();
        public Task PutAsync(string key,string value,TimeSpan ttl,CancellationToken ct)=>throw Failure();
        public Task RemoveAsync(string key,CancellationToken ct)=>throw Failure();
    }
}
