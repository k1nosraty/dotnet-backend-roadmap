using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ProxyTests
{
    [Theory]
    [InlineData("192.0.2.10", "https", 200)]
    [InlineData("192.0.2.11", "https", 307)]
    [InlineData("192.0.2.10", "http", 307)]
    [InlineData("::ffff:192.0.2.10", "https", 200)]
    public async Task Only_the_configured_peer_can_set_the_original_scheme(string peer, string scheme, int status)
    {
        using var factory = new ProxyFactory(peer);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", scheme);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.25");
        using var response = await client.GetAsync("/info");
        Assert.Equal(status, (int)response.StatusCode);
        if (status == 200)
        {
            var info = await response.Content.ReadFromJsonAsync<Info>();
            Assert.NotNull(info); Assert.Equal("https", info.Scheme); Assert.True(info.Secure);
        }
        else Assert.Equal("https://localhost:8443/info", response.Headers.Location!.ToString());
    }
    [Fact]
    public async Task Forwarded_host_is_not_an_allowed_host_bypass()
    {
        using var factory = new ProxyFactory("192.0.2.10");
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/info");
        request.Headers.Host = "attacker.invalid";
        request.Headers.Add("X-Forwarded-Host", "localhost");
        request.Headers.Add("X-Forwarded-Proto", "https");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    private sealed record Info(string Scheme, bool Secure);
    private sealed class ProxyFactory(string peer) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["ReverseProxy:KnownIp"] = "192.0.2.10", ["AllowedHosts"] = "localhost" }));
            builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter>(new TestPeer(peer)));
        }
    }
    // Test-only connection injection; production never reads a peer identity from an HTTP header.
    private sealed class TestPeer(string peer) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, continuation) => { context.Connection.RemoteIpAddress = IPAddress.Parse(peer); return continuation(context); });
            next(app);
        };
    }
}
