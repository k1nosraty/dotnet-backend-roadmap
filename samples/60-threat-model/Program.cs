using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using SupportDesk.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication("ClosedGate").AddScheme<AuthenticationSchemeOptions, ClosedGate>("ClosedGate", _ => { });
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/api/tickets", () => Results.Ok(new[] { new { id = 1, title = "Private ticket" } }));
app.Run();
