using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Hosting;
using SupportDesk.Persistence;
using Testcontainers.PostgreSql;
using Xunit;
[Trait("Category","Docker")]
public sealed class CapstoneTests(DeskFixture fixture):IClassFixture<DeskFixture>
{
    [Fact]
    public async Task Anonymous_spoof_and_tampered_token_are_denied()
    {
        await fixture.Reset();using var client=fixture.Factory.Client();client.DefaultRequestHeaders.Add("X-Test-Actor","agent");
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/requests")).StatusCode);
        client.DefaultRequestHeaders.Authorization=new("Bearer","arbitrary-text");
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/requests")).StatusCode);
        using var bad=await client.PostAsJsonAsync("/auth/login",new {email="missing@alpha.test",password="invalid"});
        Assert.Equal(HttpStatusCode.Unauthorized,bad.StatusCode);
    }
    [Fact]
    public async Task Racing_same_key_creates_one_request_and_one_audit_and_changed_payload_conflicts()
    {
        await fixture.Reset();using var owner=await fixture.As("requester@alpha.test");owner.DefaultRequestHeaders.Add("Idempotency-Key",Guid.NewGuid().ToString());
        var responses=await Task.WhenAll(Enumerable.Range(0,6).Select(_=>owner.PostAsJsonAsync("/requests",new {title="Printer",priority="urgent"})));
        Assert.Single(responses,r=>r.StatusCode==HttpStatusCode.Created);Assert.Equal(5,responses.Count(r=>r.StatusCode==HttpStatusCode.OK));
        var ids=new List<Guid>();foreach(var response in responses){ids.Add((await response.Content.ReadFromJsonAsync<RequestView>())!.Id);response.Dispose();}
        Assert.Single(ids.Distinct());
        Assert.Equal(HttpStatusCode.Conflict,(await owner.PostAsJsonAsync("/requests",new {title="Different",priority="urgent"})).StatusCode);
        await using var scope=fixture.Factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<DeskContext>();
        Assert.Equal(1,await db.Requests.CountAsync());Assert.Equal(1,await db.Audits.CountAsync());
    }
    [Fact]
    public async Task Lifecycle_claim_close_reopen_and_second_close_preserves_rules_and_audit()
    {
        await fixture.Reset();using var owner=await fixture.As("requester@alpha.test");using var agent=await fixture.As("agent@alpha.test");
        var row=await Create(owner);
        Assert.Equal(HttpStatusCode.Forbidden,(await owner.PostAsJsonAsync($"/requests/{row.Id}/claim",new {version=1})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await agent.PostAsJsonAsync($"/requests/{row.Id}/claim",new {version=1})).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await agent.PostAsJsonAsync($"/requests/{row.Id}/close",new {version=2,resolution="no"})).StatusCode);
        var races=await Task.WhenAll(Enumerable.Range(0,6).Select(_=>agent.PostAsJsonAsync($"/requests/{row.Id}/close",new {version=2,resolution="Driver updated"})));
        Assert.Single(races,r=>r.StatusCode==HttpStatusCode.OK);Assert.Equal(5,races.Count(r=>r.StatusCode==HttpStatusCode.Conflict));foreach(var response in races)response.Dispose();
        Assert.Equal(HttpStatusCode.OK,(await owner.PostAsJsonAsync($"/requests/{row.Id}/reopen",new {version=3})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await agent.PostAsJsonAsync($"/requests/{row.Id}/claim",new {version=4})).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await agent.PostAsJsonAsync($"/requests/{row.Id}/close",new {version=5,resolution="Verified driver"})).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,(await owner.PostAsJsonAsync($"/requests/{row.Id}/reopen",new {version=6})).StatusCode);
        var events=await owner.GetFromJsonAsync<AuditView[]>($"/requests/{row.Id}/audit");Assert.NotNull(events);Assert.Equal(6,events.Length);
        Assert.DoesNotContain("Driver updated",await owner.GetStringAsync($"/requests/{row.Id}/audit"));
    }
    [Fact]
    public async Task Ownership_tenant_filter_and_unknown_body_fields_are_enforced()
    {
        await fixture.Reset();using var owner=await fixture.As("requester@alpha.test");var row=await Create(owner);
        foreach(var email in new[]{"peer@alpha.test","requester@beta.test"})
        {
            using var stranger=await fixture.As(email);
            Assert.Equal(HttpStatusCode.NotFound,(await stranger.GetAsync($"/requests/{row.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,(await stranger.GetAsync($"/requests/{row.Id}/audit")).StatusCode);
            Assert.Empty((await stranger.GetFromJsonAsync<RequestView[]>("/requests"))!);
        }
        owner.DefaultRequestHeaders.Remove("Idempotency-Key");owner.DefaultRequestHeaders.Add("Idempotency-Key",Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.BadRequest,(await owner.PostAsJsonAsync("/requests",new {title="Injected",priority="normal",tenant=Guid.NewGuid()})).StatusCode);
    }
    [Fact]
    public async Task Server_side_grant_revocation_affects_existing_token()
    {
        await fixture.Reset();using var owner=await fixture.As("requester@alpha.test");var row=await Create(owner);using var agent=await fixture.As("agent@alpha.test");
        Assert.Equal(HttpStatusCode.OK,(await agent.GetAsync($"/requests/{row.Id}")).StatusCode);
        var own=await Create(agent);
        await using(var scope=fixture.Factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<DeskContext>().Users.Where(u=>u.Email=="agent@alpha.test").ExecuteUpdateAsync(set=>set.SetProperty(u=>u.IsAgent,false));
        Assert.Equal(HttpStatusCode.NotFound,(await agent.GetAsync($"/requests/{row.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await agent.PostAsJsonAsync($"/requests/{row.Id}/claim",new {version=1})).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await agent.PostAsJsonAsync($"/requests/{own.Id}/claim",new {version=1})).StatusCode);
    }
    [Fact]
    public async Task Audit_failure_after_first_database_write_rolls_back_request()
    {
        await fixture.Reset();using var owner=await fixture.As("requester@alpha.test");owner.DefaultRequestHeaders.Add("Idempotency-Key",Guid.NewGuid().ToString());fixture.Fault.Fail=true;
        try
        {
            using var response=await owner.PostAsJsonAsync("/requests",new {title="Rollback",priority="normal"});
            Assert.Equal(HttpStatusCode.InternalServerError,response.StatusCode);
            Assert.DoesNotContain("synthetic audit",await response.Content.ReadAsStringAsync());
        }
        finally{fixture.Fault.Fail=false;}
        await using var scope=fixture.Factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<DeskContext>();
        Assert.Equal(0,await db.Requests.CountAsync());Assert.Equal(0,await db.Audits.CountAsync());
    }
    [Fact]
    public async Task New_host_reuses_persisted_data_and_protected_token_keys()
    {
        await fixture.Reset();using var owner=await fixture.As("requester@alpha.test");var row=await Create(owner);
        using var second=new DeskFactory(fixture.Connection,fixture.Keys,new());using var client=second.Client();
        client.DefaultRequestHeaders.Authorization=owner.DefaultRequestHeaders.Authorization;
        Assert.Equal(row.Id,(await client.GetFromJsonAsync<RequestView>($"/requests/{row.Id}"))!.Id);
    }
    [Fact]
    public async Task Pagination_and_parameterized_title_keep_a_bounded_contract()
    {
        await fixture.Reset();using var owner=await fixture.As("requester@alpha.test");
        owner.DefaultRequestHeaders.Add("Idempotency-Key",Guid.NewGuid().ToString());
        const string title="Printer '; DROP TABLE requests; --";
        using var response=await owner.PostAsJsonAsync("/requests",new {title,priority="normal"});
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);Assert.Equal(title,(await response.Content.ReadFromJsonAsync<RequestView>())!.Title);
        Assert.Equal(HttpStatusCode.BadRequest,(await owner.GetAsync("/requests?page=2147483647&size=50")).StatusCode);
        Assert.Single((await owner.GetFromJsonAsync<RequestView[]>("/requests?page=1&size=1"))!);
    }
    [Fact]
    public async Task Api_rate_limit_is_executed_by_the_real_pipeline()
    {
        using var isolated=new DeskFactory(fixture.Connection,fixture.Keys,new());using var client=isolated.Client();
        var responses=await Task.WhenAll(Enumerable.Range(0,105).Select(_=>client.GetAsync("/requests")));
        Assert.Equal(100,responses.Count(r=>r.StatusCode==HttpStatusCode.Unauthorized));
        Assert.Equal(5,responses.Count(r=>(int)r.StatusCode==429));foreach(var response in responses)response.Dispose();
    }
    private static async Task<RequestView> Create(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("Idempotency-Key");client.DefaultRequestHeaders.Add("Idempotency-Key",Guid.NewGuid().ToString());
        using var response=await client.PostAsJsonAsync("/requests",new {title="Printer",priority="urgent"});
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);return(await response.Content.ReadFromJsonAsync<RequestView>())!;
    }
}
public sealed class DeskFixture:IAsyncLifetime
{
    private readonly PostgreSqlContainer container=new PostgreSqlBuilder("postgres:18.6").WithDatabase("desk").WithUsername("course").WithPassword(Guid.NewGuid().ToString("N")).Build();
    private readonly Dictionary<string,string> tokens=new();
    public string Password {get;}="Aa9!"+Guid.NewGuid().ToString("N");
    public string Connection {get;private set;}="";
    public string Keys {get;}=Path.Combine(Path.GetTempPath(),"capstone-keys-"+Guid.NewGuid().ToString("N"));
    public AuditFault Fault {get;}=new();
    public DeskFactory Factory {get;private set;}=null!;
    public async Task InitializeAsync()
    {
        await container.StartAsync();var settings=new NpgsqlConnectionStringBuilder(container.GetConnectionString()){SearchPath="capstone_"+Guid.NewGuid().ToString("N")};Connection=settings.ConnectionString;
        Factory=new(Connection,Keys,Fault);await Bootstrap.InitializeAsync(Factory.Services,Password);
    }
    public async Task<HttpClient> As(string email)
    {
        var client=Factory.Client();
        if(!tokens.TryGetValue(email,out var token))
        {
            using var response=await client.PostAsJsonAsync("/auth/login",new {email,password=Password});
            Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            var payload=await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();token=payload.GetProperty("accessToken").GetString()!;tokens[email]=token;
        }
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);return client;
    }
    public async Task Reset()
    {
        Fault.Fail=false;await using var scope=Factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<DeskContext>();
        await db.Audits.ExecuteDeleteAsync();await db.Requests.ExecuteDeleteAsync();
        await db.Users.Where(u=>u.Email=="agent@alpha.test").ExecuteUpdateAsync(set=>set.SetProperty(u=>u.IsAgent,true));
    }
    public async Task DisposeAsync(){await Factory.DisposeAsync();await container.DisposeAsync();if(Directory.Exists(Keys))Directory.Delete(Keys,true);}
}
public sealed class DeskFactory(string connection,string keys,AuditFault fault):WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?> { ["Desk:Connection"]=connection,["Desk:Keys"]=keys,["Desk:LabHttp"]="false" }));
        builder.ConfigureTestServices(services=>services.AddSingleton<ISaveChangesInterceptor>(fault));
    }
    public HttpClient Client()=>CreateClient(new() {BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
}
public sealed class AuditFault:SaveChangesInterceptor
{
    public bool Fail;
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,InterceptionResult<int> result,CancellationToken cancellationToken=default)
    {
        if(Fail&&eventData.Context!.ChangeTracker.Entries<DeskAudit>().Any(e=>e.State==EntityState.Added))throw new InvalidOperationException("synthetic audit failure");
        return ValueTask.FromResult(result);
    }
}
