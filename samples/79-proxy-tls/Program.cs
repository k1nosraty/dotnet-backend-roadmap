using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((options, configuration) =>
{
    var configuredIp = configuration["ReverseProxy:KnownIp"] ?? "127.0.0.1";
    if (!IPAddress.TryParse(configuredIp, out var proxy))
        throw new InvalidOperationException("ReverseProxy:KnownIp must be an IP address.");
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.KnownProxies.Add(proxy);
    if (proxy.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        options.KnownProxies.Add(proxy.MapToIPv6());
}).ValidateOnStart();
builder.Services.AddHttpsRedirection(options => options.HttpsPort = 8443);
var app = builder.Build();
app.UseForwardedHeaders(); // Must run before redirect decisions.
app.UseHttpsRedirection();
app.MapGet("/info", (HttpContext context) => Results.Ok(new { scheme = context.Request.Scheme, secure = context.Request.IsHttps }));
app.Run();
public partial class Program { }
