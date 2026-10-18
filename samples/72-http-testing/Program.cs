using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using SupportDesk.Application;
using SupportDesk.Endpoints;
using SupportDesk.Hosting;
using SupportDesk.Infrastructure;
var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);
builder.Services.AddAuthentication("Closed").AddScheme<AuthenticationSchemeOptions, ClosedGate>("Closed", _ => { });
builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddSingleton<ITicketStore, MemoryTicketStore>();
builder.Services.AddScoped<Tickets>();
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(context => Results.Problem(statusCode: 500, title: "Storage operation failed.").ExecuteAsync(context)));
app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapTickets();
app.Run();
public partial class Program { }
