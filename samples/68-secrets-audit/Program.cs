using System.Net;
using SupportDesk.Audit;
using SupportDesk.Authorization;
using SupportDesk.Abuse;
using SupportDesk.Tickets;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Accounts;
using SupportDesk.Endpoints;
using SupportDesk.Hosting;
using SupportDesk.Persistence;

bool initialize = args.Contains("--init-lab");
bool review = args.Contains("--review");
var builder = WebApplication.CreateBuilder(args.Where(a => a is not ("--init-lab" or "--review")).ToArray());
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 16 * 1024);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("SupportDesk.Audit.SecurityEvents", LogLevel.Information);
builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
// Keep binding failures as client errors in Development as well as Production.
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
var settings = LabSettings.Read();
string connection = settings.Connection;
string keys = settings.Keys;
bool insecureLab = Environment.GetEnvironmentVariable("TEAMDESK_INSECURE_LAB") == "1";
if (insecureLab && !builder.Environment.IsDevelopment()) throw new InvalidOperationException("Insecure lab transport is Development-only.");
AccountServices.AddAccounts(builder.Services, o => o.UseNpgsql(connection));
builder.Services.AddTicketAccess();
builder.Services.AddSingleton<SecurityEvents>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, AuditReadHandler>();
builder.Services.AddAuthorization(o => o.AddPolicy("ViewAudit", p => p.RequireAuthenticatedUser().AddRequirements(new AuditReadRequirement())));
builder.Services.AddAbuseLimits();
builder.Services.AddCors(options => options.AddPolicy("Portal", policy => policy
    .WithOrigins("https://portal.example.test").WithMethods("GET", "POST")
    .WithHeaders("Authorization", "Content-Type", "X-CSRF-TOKEN")));
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, TicketOwnerHandler>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keys)).SetApplicationName("TeamDesk.AccessLab69");
builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "TeamDesk.Auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = insecureLab ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    o.SlidingExpiration = false;
    o.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));
builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, o =>
{
    o.BearerTokenExpiration = TimeSpan.FromMinutes(5);
    o.RefreshTokenExpiration = TimeSpan.FromMinutes(20);
});
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.SecurePolicy = insecureLab ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("CookieOnly", p => p.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme).RequireAuthenticatedUser());
    o.AddPolicy("BearerOnly", p => p.AddAuthenticationSchemes(IdentityConstants.BearerScheme).RequireAuthenticatedUser());
});
var app = builder.Build();
if (initialize)
{
    if (!insecureLab) throw new InvalidOperationException("--init-lab requires the explicit local Development lab mode.");
    await LabProvisioning.InitializeAsync(app.Services, connection);
    return;
}
app.UseExceptionHandler(handler => handler.Run(context =>
{
    context.RequestServices.GetRequiredService<SecurityEvents>().Record(SecurityCode.ServerFailure);
    return Results.Problem(statusCode: 500, title: "Server operation failed.").ExecuteAsync(context);
}));
app.Use(async (context, next) =>
{
    if (!context.Request.IsHttps && !(insecureLab && context.Connection.RemoteIpAddress is { } ip && IPAddress.IsLoopback(ip)))
    { context.Response.StatusCode = 400; return; }
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next();
});
app.UseRouting();
app.UseCors("Portal");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/health/ready", async (AccountContext db, CancellationToken ct) =>
{
    try { _ = await db.Users.Select(u => u.Id).Take(1).ToArrayAsync(ct); return Results.Ok(new { status = "ready" }); }
    catch (Exception ex) when (ex is Npgsql.NpgsqlException or InvalidOperationException or TimeoutException)
    { return (IResult)Results.Problem(statusCode: 503, title: "Account store unavailable."); }
}).AllowAnonymous();
app.MapAuthenticationLab();
app.MapGet("/api/private-default", () => Results.Ok(new { status = "authenticated" }));
app.MapGet("/api/staff/report", () => Results.Ok(new { status = "staff-authorized" })).RequireAuthorization("ManageTickets").RequireRateLimiting("tickets");
app.MapOwnedTickets();
app.MapAudit();
if (review) throw new InvalidOperationException("Use lesson 70 for --review.");
app.Run();
