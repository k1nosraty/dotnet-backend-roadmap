using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SupportDesk.Application;
using SupportDesk.Domain;
using Xunit;
namespace SupportDesk.Tests;
public sealed class HttpTests
{
    [Fact]
    public async Task Anonymous_is_challenged_without_redirect()
    {
        using var factory = new TicketFactory(); using var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/tickets", new { title = "Network" })).StatusCode);
    }
    [Fact]
    public async Task Created_location_reads_the_same_resource_and_other_owner_is_hidden()
    {
        using var factory = new TicketFactory(); using var alice = factory.As("alice"); using var bob = factory.As("bob");
        using var made = await alice.PostAsJsonAsync("/api/tickets", new { title = " اختلال شبکه " });
        Assert.Equal(HttpStatusCode.Created, made.StatusCode);
        var created = await made.Content.ReadFromJsonAsync<TicketView>();
        Assert.NotNull(created); Assert.Equal("اختلال شبکه", created.Title);
        Assert.NotNull(made.Headers.Location);
        Assert.Equal(created, await alice.GetFromJsonAsync<TicketView>(made.Headers.Location));
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync(made.Headers.Location)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.PostAsync(made.Headers.Location+"/start", null)).StatusCode);
        Assert.Equal("Open", (await alice.GetFromJsonAsync<TicketView>(made.Headers.Location))!.Status);
    }
    [Theory] [InlineData("{broken")] [InlineData("{\"title\":\"ab\"}")]
    [InlineData("{\"title\":\"Network\",\"ownerId\":\"bob\"}")]
    public async Task Invalid_json_or_contract_returns_400(string json)
    {
        using var factory = new TicketFactory(); using var client = factory.As("alice");
        using var response = await client.PostAsync("/api/tickets", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task Real_pipeline_maps_state_conflict_and_successful_resolution()
    {
        using var factory = new TicketFactory(); using var client = factory.As("alice");
        using var made = await client.PostAsJsonAsync("/api/tickets", new { title = "Network" });
        string path = made.Headers.Location!.ToString();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path+"/resolve", new { resolution = "Cable fixed" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync(path+"/start", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path+"/resolve", new { resolution = "ab" })).StatusCode);
        Assert.Equal("InProgress", (await client.GetFromJsonAsync<TicketView>(path))!.Status);
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => client.PostAsJsonAsync(path+"/resolve", new { resolution = "Fixed cable "+i })));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Equal(7, responses.Count(x => x.StatusCode == HttpStatusCode.Conflict));
        foreach (var response in responses) response.Dispose();
        Assert.Equal("Closed", (await client.GetFromJsonAsync<TicketView>(path))!.Status);
    }
    [Fact]
    public async Task Factory_state_is_not_shared_between_tests_or_hosts()
    {
        using var first = new TicketFactory(); using var alice = first.As("alice");
        using var made = await alice.PostAsJsonAsync("/api/tickets", new { title = "Network" });
        using var second = new TicketFactory(); using var clean = second.As("alice");
        Assert.Equal(HttpStatusCode.NotFound, (await clean.GetAsync(made.Headers.Location)).StatusCode);
    }
    [Fact]
    public async Task Replacing_storage_with_outage_checks_error_pipeline_without_real_database()
    {
        using var factory = new FailingFactory(); using var client = factory.As("alice");
        using var response = await client.PostAsJsonAsync("/api/tickets", new { title = "Network" });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PrivateStoreDiagnostic", body);
        Assert.DoesNotContain("IOException", body);
    }
    private sealed class FailingFactory : TicketFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            { services.RemoveAll<ITicketStore>(); services.AddSingleton<ITicketStore, FailingStore>(); });
        }
    }
    private sealed class FailingStore : ITicketStore
    {
        public Task<TicketSnapshot> CreateAsync(TicketTitle title, string owner, CancellationToken ct) => throw new IOException("PrivateStoreDiagnostic");
        public Task<TicketSnapshot?> LoadAsync(Guid id, CancellationToken ct) => throw new IOException("PrivateStoreDiagnostic");
        public Task<SaveOutcome> SaveAsync(TicketSnapshot expected, Ticket changed, CancellationToken ct) => throw new IOException("PrivateStoreDiagnostic");
    }
}
