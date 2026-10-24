using Npgsql;
using SupportDesk;
using Testcontainers.PostgreSql;
using Xunit;
[Trait("Category","Docker")]
public sealed class QueueTests : IClassFixture<Database>
{
    private readonly Database db;
    public QueueTests(Database db)=>this.db=db;
    [Fact]
    public async Task Duplicate_enqueue_and_racing_consumers_have_one_database_effect()
    {
        await db.Reset(); var id=Guid.NewGuid(); await db.Store.EnqueueAsync(id); await db.Store.EnqueueAsync(id);
        var results=await Task.WhenAll(Enumerable.Range(0,8).Select(_=>db.Store.ClaimAsync()));
        var lease=Assert.Single(results,x=>x is not null); Assert.NotNull(lease);
        Assert.True(await db.Store.CompleteAsync(lease)); Assert.False(await db.Store.CompleteAsync(lease));
        Assert.Equal(new QueueCounts(0,0,1,0,1),await db.Store.CountsAsync());
    }
    [Fact]
    public async Task Expired_lease_can_be_reclaimed_and_old_token_cannot_ack()
    {
        await db.Reset(); await db.Store.EnqueueAsync(Guid.NewGuid()); var old=(await db.Store.ClaimAsync())!;
        await db.Sql("UPDATE jobs SET lease_until=clock_timestamp()-interval '1 second'");
        var fresh=(await db.Store.ClaimAsync())!;
        Assert.NotEqual(old.Token,fresh.Token); Assert.Equal(2,fresh.Attempt);
        Assert.False(await db.Store.CompleteAsync(old)); Assert.True(await db.Store.CompleteAsync(fresh));
        Assert.Equal(1,(await db.Store.CountsAsync()).Effects);
    }
    [Fact]
    public async Task Retry_exhaustion_moves_to_dead_without_effect()
    {
        await db.Reset(); await db.Store.EnqueueAsync(Guid.NewGuid());
        for(var attempt=1;attempt<=3;attempt++)
        {
            var lease=await db.Store.ClaimAsync(); Assert.NotNull(lease); Assert.Equal(attempt,lease.Attempt);
            Assert.True(await db.Store.FailAsync(lease)); await db.Sql("UPDATE jobs SET available_at=clock_timestamp()-interval '1 second'");
        }
        Assert.Null(await db.Store.ClaimAsync()); Assert.Equal(new QueueCounts(0,0,0,1,0),await db.Store.CountsAsync());
    }
    [Fact]
    public async Task Last_attempt_crash_is_dead_after_lease_expiry()
    {
        await db.Reset(); await db.Store.EnqueueAsync(Guid.NewGuid());
        for(var attempt=1;attempt<=3;attempt++)
        { Assert.NotNull(await db.Store.ClaimAsync()); await db.Sql("UPDATE jobs SET lease_until=clock_timestamp()-interval '1 second'"); }
        Assert.Null(await db.Store.ClaimAsync()); Assert.Equal(1,(await db.Store.CountsAsync()).Dead);
    }
    [Fact]
    public async Task New_data_source_after_restart_reads_the_same_pending_job()
    {
        await db.Reset(); var id=Guid.NewGuid(); await db.Store.EnqueueAsync(id);
        await using var fresh=NpgsqlDataSource.Create(db.Connection);
        var lease=await new QueueStore(fresh).ClaimAsync(); Assert.NotNull(lease); Assert.Equal(id,lease.Id);
    }
    [Fact]
    public async Task Invalid_id_and_cancelled_enqueue_do_not_create_jobs()
    {
        await db.Reset(); await Assert.ThrowsAsync<ArgumentException>(()=>db.Store.EnqueueAsync(Guid.Empty));
        using var cancelled=new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>db.Store.EnqueueAsync(Guid.NewGuid(),cancelled.Token));
        Assert.Equal(new QueueCounts(0,0,0,0,0),await db.Store.CountsAsync());
    }
}
public sealed class Database : IAsyncLifetime
{
    private readonly PostgreSqlContainer container=new PostgreSqlBuilder("postgres:18.6").WithDatabase("job_lab")
        .WithUsername("course").WithPassword(Guid.NewGuid().ToString("N")).Build();
    private NpgsqlDataSource data=null!;
    public string Connection=>container.GetConnectionString();
    public QueueStore Store { get; private set; }=null!;
    public async Task InitializeAsync() { await container.StartAsync(); data=NpgsqlDataSource.Create(Connection); Store=new(data); await Store.InitializeAsync(); }
    public Task Reset()=>Sql("TRUNCATE effects,jobs");
    public async Task Sql(string text) { await using var command=data.CreateCommand(text); await command.ExecuteNonQueryAsync(); }
    public async Task DisposeAsync() { await data.DisposeAsync(); await container.DisposeAsync(); }
}
