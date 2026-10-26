using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;
public sealed class OidcTests
{
    [Fact] public async Task Challenge_requests_code_pkce_state_nonce_and_exact_callback()
    {
        using var factory=new ClientFactory();using var client=factory.Client();var flow=await Start(client);
        Assert.Equal("code",flow["response_type"]);Assert.Equal("S256",flow["code_challenge_method"]);
        Assert.Equal("https://localhost/signin-oidc",flow["redirect_uri"]);
        Assert.Contains("openid",flow["scope"]);Assert.False(string.IsNullOrWhiteSpace(flow["state"]));Assert.False(string.IsNullOrWhiteSpace(flow["nonce"]));
    }
    [Fact] public async Task Real_handler_redeems_code_checks_pkce_and_nonce_and_creates_session()
    {
        using var factory=new ClientFactory();using var client=factory.Client();
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/me")).StatusCode);
        var flow=await Start(client);var code=factory.Provider.Authorize(flow);
        using var callback=await client.GetAsync(Callback(flow,code));Assert.Equal(HttpStatusCode.Redirect,callback.StatusCode);
        using var me=await client.GetAsync("/me");Assert.Equal(HttpStatusCode.OK,me.StatusCode);
        Assert.Contains("learner",await me.Content.ReadAsStringAsync());Assert.True(factory.Provider.PkceVerified);
        Assert.True(factory.Provider.DiscoveryRead&&factory.Provider.KeysRead);
        var cookie=string.Join(";",callback.Headers.GetValues("Set-Cookie"));Assert.DoesNotContain("access_token",cookie);Assert.DoesNotContain("refresh_token",cookie);
        using var replay=await client.GetAsync(Callback(flow,code));Assert.Equal(HttpStatusCode.BadRequest,replay.StatusCode);
    }
    [Theory] [InlineData("nonce")] [InlineData("audience")] [InlineData("issuer")] [InlineData("key")] [InlineData("expired")] [InlineData("pkce")]
    public async Task Invalid_protocol_response_never_creates_session(string fault)
    {
        using var factory=new ClientFactory();factory.Provider.Fault=fault;using var client=factory.Client();
        var flow=await Start(client);var code=factory.Provider.Authorize(flow);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(Callback(flow,code))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/me")).StatusCode);
    }
    [Fact] public async Task Callback_without_state_or_correlation_is_rejected()
    {
        using var factory=new ClientFactory();using var client=factory.Client();
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/signin-oidc?code=arbitrary")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/me")).StatusCode);
    }
    [Fact] public async Task Tampered_state_is_rejected()
    {
        using var factory=new ClientFactory();using var client=factory.Client();var flow=await Start(client);flow["state"]="tampered";
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync(Callback(flow,"unused"))).StatusCode);
    }
    [Fact] public async Task Local_logout_requires_csrf_and_clears_session()
    {
        using var factory=new ClientFactory();using var client=factory.Client();var flow=await Start(client);
        Assert.Equal(HttpStatusCode.Redirect,(await client.GetAsync(Callback(flow,factory.Provider.Authorize(flow)))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync("/logout",null)).StatusCode);
        var csrf=await client.GetFromJsonAsync<JsonElement>("/csrf");
        using var request=new HttpRequestMessage(HttpMethod.Post,"/logout");request.Headers.Add("X-CSRF-TOKEN",csrf.GetProperty("token").GetString());
        Assert.Equal(HttpStatusCode.NoContent,(await client.SendAsync(request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/me")).StatusCode);
    }
    [Fact] public async Task External_return_url_is_not_reflected()
    {
        using var factory=new ClientFactory();using var client=factory.Client();var flow=await Start(client,"/login?returnUrl=https://evil.test");
        using var response=await client.GetAsync(Callback(flow,factory.Provider.Authorize(flow)));
        Assert.Equal("/me",response.Headers.Location?.OriginalString);
    }
    private static async Task<Dictionary<string,string>> Start(HttpClient client,string route="/login")
    {
        using var response=await client.GetAsync(route);Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        Assert.NotNull(response.Headers.Location);return QueryHelpers.ParseQuery(response.Headers.Location.Query).ToDictionary(x=>x.Key,x=>x.Value.ToString());
    }
    private static string Callback(Dictionary<string,string> flow,string code)=>QueryHelpers.AddQueryString("/signin-oidc",new Dictionary<string,string?> { ["code"]=code,["state"]=flow["state"] });
}
public sealed class ClientFactory:WebApplicationFactory<Program>
{
    public TestProvider Provider {get;}=new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?> {
            ["Tokens:Authority"]=TestProvider.Issuer,["Oidc:ClientId"]="course-web",["Oidc:ClientSecret"]=TestProvider.Secret}));
        builder.ConfigureTestServices(services=>services.Configure<OpenIdConnectOptions>("oidc",o=>o.BackchannelHttpHandler=Provider));
    }
    public HttpClient Client()=>CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false,HandleCookies=true});
}
// Controlled IdP transport only in the test assembly; production OIDC validation is not replaced.
public sealed class TestProvider:HttpMessageHandler
{
    public const string Issuer="https://issuer.course.test/realms/course";
    public const string Secret="test-only-confidential-client-secret";
    private readonly RSA rsa=RSA.Create(2048);
    private readonly Dictionary<string,Dictionary<string,string>> codes=new();
    public string Fault {get;set;}="";
    public bool PkceVerified {get;private set;}
    public bool DiscoveryRead {get;private set;}
    public bool KeysRead {get;private set;}
    public string Authorize(Dictionary<string,string> flow)
    {
        var code=Guid.NewGuid().ToString("N");codes.Add(code,new(flow));return code;
    }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
    {
        var url=request.RequestUri!.AbsoluteUri;
        if(url==Issuer+"/.well-known/openid-configuration")
        {
            DiscoveryRead=true;return Json(new {issuer=Issuer,authorization_endpoint=Issuer+"/authorize",token_endpoint=Issuer+"/token",jwks_uri=Issuer+"/keys",
                response_types_supported=new[]{"code"},subject_types_supported=new[]{"public"},id_token_signing_alg_values_supported=new[]{"RS256"},code_challenge_methods_supported=new[]{"S256"}});
        }
        if(url==Issuer+"/keys")
        {
            KeysRead=true;var key=rsa.ExportParameters(false);return Json(new {keys=new[]{new {kty="RSA",kid="idp-key",use="sig",alg="RS256",n=Base64UrlEncoder.Encode(key.Modulus!),e=Base64UrlEncoder.Encode(key.Exponent!)}}});
        }
        if(url==Issuer+"/token")
        {
            var form=QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(ct));
            if(!codes.Remove(form["code"].ToString(),out var flow))return Json(new {error="invalid_grant"},400);
            var verifier=form["code_verifier"].ToString();var challenge=Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            PkceVerified=challenge==flow["code_challenge"]&&Fault!="pkce";
            if(!PkceVerified||form["client_id"]!="course-web"||form["client_secret"]!=Secret||form["redirect_uri"]!="https://localhost/signin-oidc")return Json(new {error="invalid_grant"},400);
            using var wrong=RSA.Create(2048);
            var now=DateTime.UtcNow;
            var token=new JwtSecurityToken(Fault=="issuer"?"https://wrong.test":Issuer,Fault=="audience"?"other-client":"course-web",
                [new Claim("sub","learner"),new Claim("name","Course Learner"),new Claim("nonce",Fault=="nonce"?"wrong-nonce":flow["nonce"])],
                now.AddMinutes(-20),now.AddMinutes(Fault=="expired"?-10:5),
                new SigningCredentials(new RsaSecurityKey(Fault=="key"?wrong:rsa){KeyId="idp-key"},SecurityAlgorithms.RsaSha256));
            token.Payload["iat"]=new DateTimeOffset(now).ToUnixTimeSeconds();
            return Json(new {access_token="opaque-test-access-token",token_type="Bearer",expires_in=300,id_token=new JwtSecurityTokenHandler().WriteToken(token)});
        }
        return Json(new {error="unexpected_backchannel_route"},404);
    }
    private static HttpResponseMessage Json(object value,int code=200)=>new((HttpStatusCode)code){Content=JsonContent.Create(value)};
    protected override void Dispose(bool disposing){if(disposing)rsa.Dispose();base.Dispose(disposing);}
}
