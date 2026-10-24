using StackExchange.Redis;
using SupportDesk;
using Testcontainers.Redis;
using Xunit;
[Trait("Category","Docker")]
public sealed class RedisTests : IAsyncLifetime
{
    private readonly RedisContainer container=new RedisBuilder("redis:8.2").Build();
    private ConnectionMultiplexer connection=null!;
    public async Task InitializeAsync() { await container.StartAsync(); connection=await ConnectionMultiplexer.ConnectAsync(container.GetConnectionString()); }
    public async Task DisposeAsync() { await connection.DisposeAsync(); await container.DisposeAsync(); }
    [Fact]
    public async Task Real_redis_roundtrip_ttl_and_invalidation()
    {
        var cache=new RedisCache(connection); var key="test:"+Guid.NewGuid().ToString("N");
        await cache.PutAsync(key,"value",TimeSpan.FromSeconds(30),default);
        Assert.Equal("value",await cache.GetAsync(key,default));
        var ttl=await connection.GetDatabase().KeyTimeToLiveAsync(key);
        Assert.NotNull(ttl); Assert.InRange(ttl.Value.TotalSeconds,0.001,30);
        await cache.RemoveAsync(key,default); Assert.Null(await cache.GetAsync(key,default));
    }
    [Fact]
    public async Task Real_redis_expiration_is_eventually_a_miss()
    {
        var cache=new RedisCache(connection); var key="test:"+Guid.NewGuid().ToString("N");
        await cache.PutAsync(key,"value",TimeSpan.FromMilliseconds(50),default);
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while(await cache.GetAsync(key,timeout.Token) is not null) await Task.Delay(20,timeout.Token);
        Assert.Null(await cache.GetAsync(key,default));
    }
}
