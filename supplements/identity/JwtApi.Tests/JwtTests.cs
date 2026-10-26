using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;
public sealed class JwtTests
{
    [Theory]
    [InlineData("issuer")] [InlineData("audience")] [InlineData("expired")] [InlineData("future")]
    [InlineData("key")] [InlineData("unsigned")] [InlineData("expiry-missing")] [InlineData("id-token")]
    [InlineData("subject-missing")] [InlineData("tampered")] [InlineData("malformed")]
    public async Task Invalid_credentials_return_401(string fault)
    {
        using var factory=new ApiFactory();using var client=factory.Client(factory.Token(fault));
        using var response=await client.GetAsync("/api/tickets");Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
        Assert.DoesNotContain("eyJ",await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task Valid_signed_scoped_access_token_reads_api()
    {
        using var factory=new ApiFactory();using var client=factory.Client(factory.Token());
        using var response=await client.GetAsync("/api/tickets");Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Contains("Synthetic ticket",await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task Valid_identity_without_required_scope_gets_403()
    {
        using var factory=new ApiFactory();using var client=factory.Client(factory.Token("scope"));
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/tickets")).StatusCode);
    }
    [Fact] public async Task Anonymous_or_query_string_token_does_not_authenticate()
    {
        using var factory=new ApiFactory();using var client=factory.Client();client.DefaultRequestHeaders.Add("X-Role","admin");
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/me?access_token="+factory.Token())).StatusCode);
    }
    [Fact] public async Task Http_transport_is_rejected()
    {
        using var factory=new ApiFactory();using var client=factory.CreateClient(new(){BaseAddress=new Uri("http://localhost"),AllowAutoRedirect=false});
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/health")).StatusCode);
    }
    [Fact] public void Configuration_rejects_non_https_authority()
    {
        using var factory=new ApiFactory("http://issuer.course.test");Assert.ThrowsAny<Exception>(()=>factory.Client());
    }
}
public sealed class ApiFactory(string authority="https://issuer.course.test"):WebApplicationFactory<Program>
{
    private readonly RSA rsa=RSA.Create(2048);
    public const string Audience="course-api";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?> {
            ["Tokens:Authority"]=authority,["Tokens:Audience"]=Audience}));
        builder.ConfigureTestServices(services=>services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme,o=>
        {
            var metadata=new OpenIdConnectConfiguration {Issuer=authority};metadata.SigningKeys.Add(new RsaSecurityKey(rsa){KeyId="test-key"});
            o.ConfigurationManager=new StaticConfigurationManager<OpenIdConnectConfiguration>(metadata);
        }));
    }
    public HttpClient Client(string? token=null)
    {
        var client=CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        if(token is not null)client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);return client;
    }
    // Issuance belongs exclusively to tests. The API has no token-minting endpoint or private signing key.
    public string Token(string fault="")
    {
        if(fault=="malformed")return "not-a-token";
        using var wrong=RSA.Create(2048);
        var claims=new List<Claim>{new("sub","learner"),new("scope",fault=="scope"?"tickets.write":"profile tickets.read")};
        if(fault=="subject-missing")claims.RemoveAll(c=>c.Type=="sub");
        var jwt=new JwtSecurityToken(fault=="issuer"?"https://wrong.test":authority,
            fault=="audience"?"other-api":Audience,claims,
            notBefore:DateTime.UtcNow.AddMinutes(fault=="future"?10:-20),
            expires:DateTime.UtcNow.AddMinutes(fault=="expired"?-10:5),
            signingCredentials:fault=="unsigned"?null:new SigningCredentials(new RsaSecurityKey(fault=="key"?wrong:rsa){KeyId="test-key"},SecurityAlgorithms.RsaSha256));
        jwt.Header["typ"]=fault=="id-token"?"JWT":"at+jwt";
        if(fault=="expiry-missing")jwt.Payload.Remove("exp");
        var token=new JwtSecurityTokenHandler().WriteToken(jwt);
        if(fault=="tampered")
        {
            var parts=token.Split('.');parts[1]=Base64UrlEncoder.Encode("{\"sub\":\"attacker\",\"iss\":\""+authority+"\",\"aud\":\"course-api\",\"exp\":4102444800}");return string.Join('.',parts);
        }
        return token;
    }
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)rsa.Dispose();}
}
