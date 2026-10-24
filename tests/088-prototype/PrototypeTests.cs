using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using SupportDesk.Domain;
using Xunit;
public sealed class PrototypeTests
{
    [Fact]
    public async Task Normal_host_never_trusts_test_identity_header()
    { using var factory=new WebApplicationFactory<Program>();using var client=factory.CreateClient();client.DefaultRequestHeaders.Add("X-Test-Actor","owner");Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/requests/"+Guid.NewGuid())).StatusCode); }
    [Fact]
    public async Task Idempotency_and_cross_tenant_and_racing_claim_are_explicit()
    {
        using var factory=new TestFactory();using var owner=factory.As("owner");var key=Guid.NewGuid();owner.DefaultRequestHeaders.Add("Idempotency-Key",key.ToString());
        using var created=await owner.PostAsJsonAsync("/requests",new {title="Printer",priority="normal"});Assert.Equal(HttpStatusCode.Created,created.StatusCode);
        var view=(await created.Content.ReadFromJsonAsync<RequestView>())!;
        using var again=await owner.PostAsJsonAsync("/requests",new {title="Printer",priority="normal"});Assert.Equal(HttpStatusCode.OK,again.StatusCode);
        Assert.Equal(view.Id,(await again.Content.ReadFromJsonAsync<RequestView>())!.Id);
        using var other=factory.As("other");Assert.Equal(HttpStatusCode.NotFound,(await other.GetAsync("/requests/"+view.Id)).StatusCode);
        using var agent=factory.As("agent");
        var races=await Task.WhenAll(Enumerable.Range(0,6).Select(_=>agent.PostAsJsonAsync($"/requests/{view.Id}/claim",new {version=1})));
        Assert.Single(races,r=>r.StatusCode==HttpStatusCode.OK);Assert.Equal(5,races.Count(r=>r.StatusCode==HttpStatusCode.Conflict));foreach(var response in races)response.Dispose();
    }
    private sealed class TestFactory:WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)=>builder.ConfigureTestServices(s=>s.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions,TestAuth>("Test",_=>{}));
        public HttpClient As(string actor) {var client=CreateClient();client.DefaultRequestHeaders.Add("X-Test-Actor",actor);return client;}
    }
    private sealed class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var actor=Request.Headers["X-Test-Actor"].ToString();if(actor is not("owner" or "agent" or "other"))return Task.FromResult(AuthenticateResult.NoResult());
            var claims=new List<Claim> { new(ClaimTypes.NameIdentifier,actor),new("tenant",actor=="other"?"22222222-2222-2222-2222-222222222222":"11111111-1111-1111-1111-111111111111") };
            if(actor=="agent")claims.Add(new(ClaimTypes.Role,"Agent"));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims,Scheme.Name)),Scheme.Name)));
        }
    }
}
