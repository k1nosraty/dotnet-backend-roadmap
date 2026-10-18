using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;
namespace SupportDesk.Tests;
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("course_testing").WithUsername("course").WithPassword("Test-"+Guid.NewGuid().ToString("N")).Build();
    public string Connection { get; private set; } = "";
    public async Task InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await container.StartAsync(timeout.Token);
        var options = new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        { SearchPath = "testing74_"+Guid.NewGuid().ToString("N"), IncludeErrorDetail = false };
        Connection = options.ConnectionString;
        await using var source = NpgsqlDataSource.Create(Connection);
        await new PostgresTicketStore(source).InitializeLabAsync(timeout.Token);
    }
    public async Task DisposeAsync() => await container.DisposeAsync();
}
public sealed class DatabaseFactory(string connection) : TicketFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<NpgsqlDataSource>(); services.RemoveAll<ITicketStore>();
            services.AddSingleton(_ => NpgsqlDataSource.Create(connection));
            services.AddScoped<ITicketStore, PostgresTicketStore>();
        });
    }
}
public sealed class PostgresTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Real_store_roundtrip_and_version_survive_new_adapter()
    {
        await using var source = NpgsqlDataSource.Create(fixture.Connection);
        var store = new PostgresTicketStore(source);
        var created = await store.CreateAsync(TicketTitle.Create("اختلال شبکه"), "owner-"+Guid.NewGuid(), default);
        Assert.Equal(SaveOutcome.Saved, await store.SaveAsync(created, created.Value.Start(), default));
        var loaded = (await new PostgresTicketStore(source).LoadAsync(created.Value.Id, default))!;
        Assert.Equal(2, loaded.Version); Assert.Equal("اختلال شبکه", loaded.Value.Title.Value);
        Assert.Equal(SaveOutcome.Saved, await store.SaveAsync(loaded, loaded.Value.Resolve("Cable fixed"), default));
        Assert.Equal(TicketStatus.Closed, (await store.LoadAsync(created.Value.Id, default))!.Value.Status);
    }
    [Fact]
    public async Task Atomic_compare_and_swap_rejects_one_of_two_real_writers()
    {
        await using var source = NpgsqlDataSource.Create(fixture.Connection); var store = new PostgresTicketStore(source);
        var row = await store.CreateAsync(TicketTitle.Create("Concurrent write"), "owner-"+Guid.NewGuid(), default);
        var results = await Task.WhenAll(store.SaveAsync(row, row.Value.Start(), default), store.SaveAsync(row, row.Value.Start(), default));
        Assert.Single(results, r => r == SaveOutcome.Saved); Assert.Single(results, r => r == SaveOutcome.Conflict);
        Assert.Equal(2, (await store.LoadAsync(row.Value.Id, default))!.Version);
    }
    [Fact]
    public async Task Database_constraint_failure_rolls_back_an_earlier_insert()
    {
        await using var source = NpgsqlDataSource.Create(fixture.Connection);
        await using var connection = await source.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        Guid id = Guid.NewGuid();
        await using var first = new NpgsqlCommand("INSERT INTO tickets(id,owner_id,title,status,version) VALUES ($1,'rollback-owner','Valid title','Open',1)", connection, transaction);
        first.Parameters.AddWithValue(id); await first.ExecuteNonQueryAsync();
        await using var invalid = new NpgsqlCommand("INSERT INTO tickets(id,owner_id,title,status,version) VALUES ($1,'rollback-owner','ab','Open',1)", connection, transaction);
        invalid.Parameters.AddWithValue(Guid.NewGuid());
        var error = await Assert.ThrowsAsync<PostgresException>(() => invalid.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        await transaction.RollbackAsync();
        Assert.Null(await new PostgresTicketStore(source).LoadAsync(id, default));
    }
    [Fact]
    public async Task Parameterized_title_is_literal_and_owner_cannot_be_replaced()
    {
        await using var source = NpgsqlDataSource.Create(fixture.Connection); var store = new PostgresTicketStore(source);
        const string literal = "' OR 1=1 --";
        var row = await store.CreateAsync(TicketTitle.Create(literal), "owner-"+Guid.NewGuid(), default);
        Assert.Equal(literal, (await store.LoadAsync(row.Value.Id, default))!.Value.Title.Value);
        Assert.Equal(SaveOutcome.Missing, await store.SaveAsync(row with { OwnerId = "impostor" }, row.Value.Start(), default));
        Assert.Equal(TicketStatus.Open, (await store.LoadAsync(row.Value.Id, default))!.Value.Status);
    }
    [Fact]
    public async Task Missing_and_pre_cancelled_operations_do_not_report_success()
    {
        await using var source = NpgsqlDataSource.Create(fixture.Connection); var store = new PostgresTicketStore(source);
        Assert.Null(await store.LoadAsync(Guid.NewGuid(), default));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.CreateAsync(TicketTitle.Create("Cancelled"), "owner", cancelled.Token));
    }
    [Fact]
    public async Task Real_http_and_database_preserve_data_across_host_restart_and_hide_other_owner()
    {
        string location;
        using (var factory = new DatabaseFactory(fixture.Connection))
        using (var client = factory.As("alice"))
        {
            using var made = await client.PostAsJsonAsync("/api/tickets", new { title = "Persisted HTTP" });
            Assert.Equal(HttpStatusCode.Created, made.StatusCode); location = made.Headers.Location!.ToString();
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(location+"/start", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(location+"/resolve", new { resolution = "Stored resolution" })).StatusCode);
        }
        using var reopened = new DatabaseFactory(fixture.Connection); using var alice = reopened.As("alice"); using var bob = reopened.As("bob");
        var saved = await alice.GetFromJsonAsync<TicketView>(location);
        Assert.NotNull(saved); Assert.Equal("Closed", saved.Status); Assert.Equal("Stored resolution", saved.Resolution);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync(location)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PostAsync(location+"/start", null)).StatusCode);
    }
}
