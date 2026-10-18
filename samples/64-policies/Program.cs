using System.Net;
using SupportDesk.Authorization;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Accounts;
using SupportDesk.Endpoints;
using SupportDesk.Hosting;
using SupportDesk.Persistence;

bool initialize = args.Contains("--init-lab");
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--init-lab").ToArray());
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
string connection = Environment.GetEnvironmentVariable("SUPPORTDESK_PG") ?? throw new InvalidOperationException("Set SUPPORTDESK_PG.");
string keys = Environment.GetEnvironmentVariable("TEAMDESK_KEYS") ?? throw new InvalidOperationException("Set TEAMDESK_KEYS to a private lab key directory.");
bool insecureLab = Environment.GetEnvironmentVariable("TEAMDESK_INSECURE_LAB") == "1";
if (insecureLab && !builder.Environment.IsDevelopment()) throw new InvalidOperationException("Insecure lab transport is Development-only.");
AccountServices.AddAccounts(builder.Services, o => o.UseNpgsql(connection));
builder.Services.AddTicketAccess();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keys)).SetApplicationName("TeamDesk.AccessLab65");
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
app.UseExceptionHandler(handler => handler.Run(context => Results.Problem(statusCode: 500, title: "Server operation failed.").ExecuteAsync(context)));
app.Use(async (context, next) =>
{
    if (!context.Request.IsHttps && !(insecureLab && context.Connection.RemoteIpAddress is { } ip && IPAddress.IsLoopback(ip)))
    { context.Response.StatusCode = 400; return; }
    context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseAuthentication();
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
app.MapGet("/api/staff/report", () => Results.Ok(new { status = "staff-authorized" })).RequireAuthorization("ManageTickets");
app.Run();
