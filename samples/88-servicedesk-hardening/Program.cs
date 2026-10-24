using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.HttpOverrides;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using SupportDesk.Application;
using SupportDesk.Endpoints;
using SupportDesk.Hosting;
using SupportDesk.Persistence;
var initialize=args.Contains("--init-lab");
var builder=WebApplication.CreateBuilder(args.Where(a=>a!="--init-lab").ToArray());
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=16*1024);
builder.Logging.ClearProviders();builder.Logging.AddJsonConsole();builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime",LogLevel.Information);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore",LogLevel.None);
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",LogLevel.None);
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(o=>o.ThrowOnBadRequest=false);
builder.Services.ConfigureHttpJsonOptions(o=>o.SerializerOptions.UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow);
builder.Services.AddOptions<DeskSettings>().Configure<IConfiguration,IHostEnvironment>(DeskSettings.Load).ValidateOnStart();
builder.Services.AddDbContext<DeskContext>((services,options)=>
{
    options.UseNpgsql(services.GetRequiredService<IOptions<DeskSettings>>().Value.Connection);
    options.AddInterceptors(services.GetServices<ISaveChangesInterceptor>());
});
builder.Services.AddIdentityApiEndpoints<DeskUser>(o=>
{
    o.User.RequireUniqueEmail=true;o.SignIn.RequireConfirmedEmail=true;o.Password.RequiredLength=12;
    o.Lockout.MaxFailedAccessAttempts=5;o.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(2);
}).AddEntityFrameworkStores<DeskContext>();
builder.Services.AddAuthentication(o=>{o.DefaultAuthenticateScheme=IdentityConstants.BearerScheme;o.DefaultChallengeScheme=IdentityConstants.BearerScheme;});
builder.Services.AddAuthorization(o=>o.FallbackPolicy=new AuthorizationPolicyBuilder(IdentityConstants.BearerScheme).RequireAuthenticatedUser().Build());
builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme,o=>{o.BearerTokenExpiration=TimeSpan.FromMinutes(5);o.RefreshTokenExpiration=TimeSpan.FromMinutes(5);});
builder.Services.AddDataProtection().SetApplicationName("ServiceDesk.Capstone");
builder.Services.AddOptions<KeyManagementOptions>().Configure<IOptions<DeskSettings>>((options,settings)=>
{
    var directory=new DirectoryInfo(settings.Value.Keys);directory.Create();
    options.XmlRepository=new Microsoft.AspNetCore.DataProtection.Repositories.FileSystemXmlRepository(directory,
        Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
});
builder.Services.AddSingleton(TimeProvider.System);builder.Services.AddScoped<DeskService>();
builder.Services.AddRateLimiter(o=>
{
    o.RejectionStatusCode=429;
    o.AddFixedWindowLimiter("login",limiter=>{limiter.PermitLimit=10;limiter.Window=TimeSpan.FromMinutes(1);limiter.QueueLimit=0;});
    o.AddPolicy("api",context=>RateLimitPartition.GetFixedWindowLimiter(context.User.FindFirstValue(ClaimTypes.NameIdentifier)??"anonymous",
        _=>new FixedWindowRateLimiterOptions {PermitLimit=100,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
});
builder.Services.AddOptions<ForwardedHeadersOptions>().Configure<IConfiguration>((options,config)=>
{
    options.KnownIPNetworks.Clear();options.KnownProxies.Clear();
    if(config["Desk:ProxyIp"] is string text)
    {
        if(!IPAddress.TryParse(text,out var proxy))throw new InvalidOperationException("Invalid Desk:ProxyIp.");
        options.ForwardedHeaders=ForwardedHeaders.XForwardedFor|ForwardedHeaders.XForwardedProto;
        options.ForwardLimit=1;options.KnownProxies.Add(proxy);
        if(proxy.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork)options.KnownProxies.Add(proxy.MapToIPv6());
    }
}).ValidateOnStart();
var app=builder.Build();
var settings=app.Services.GetRequiredService<IOptions<DeskSettings>>().Value;
if(initialize)
{
    if(!settings.LabHttp||!app.Environment.IsDevelopment())throw new InvalidOperationException("Bootstrap requires explicit Development LabHttp mode.");
    var password=Environment.GetEnvironmentVariable("CAPSTONE_LAB_PASSWORD")??throw new InvalidOperationException("Set CAPSTONE_LAB_PASSWORD privately.");
    await Bootstrap.InitializeAsync(app.Services,password);return;
}
app.UseExceptionHandler(handler=>handler.Run(context=>Results.Problem(statusCode:500,title:"Service operation failed.").ExecuteAsync(context)));
app.UseForwardedHeaders();
app.Use(async(context,next)=>
{
    context.Response.Headers.CacheControl="no-store";context.Response.Headers["X-Content-Type-Options"]="nosniff";
    var loopback=context.Connection.RemoteIpAddress is {} ip&&IPAddress.IsLoopback(ip);
    if(!context.Request.IsHttps&&!(settings.LabHttp&&loopback)){context.Response.StatusCode=400;return;}
    await next();
});
app.UseAuthentication();app.UseRateLimiter();app.UseAuthorization();
app.MapGet("/health/live",()=>Results.Text("Healthy")).AllowAnonymous();
app.MapGet("/health/ready",async(DeskContext db,CancellationToken ct)=>
{
    using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);deadline.CancelAfter(TimeSpan.FromSeconds(3));
    try {await db.Requests.AsNoTracking().Take(1).Select(r=>r.Id).ToArrayAsync(deadline.Token);return Results.Text("Healthy");}
    catch(Exception ex) when(ex is Npgsql.NpgsqlException or OperationCanceledException){return Results.StatusCode(503);}
}).AllowAnonymous();
var commit=typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a=>a.Key=="CourseCommit").Value;
app.MapGet("/version",()=>Results.Ok(new {service="servicedesk",version="1.0.0",commit})).AllowAnonymous();
app.MapDesk();app.Run();
public partial class Program { }
