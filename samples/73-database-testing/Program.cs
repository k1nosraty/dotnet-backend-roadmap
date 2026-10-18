using Microsoft.AspNetCore.Authentication;
using Npgsql;
using Microsoft.AspNetCore.Authorization;
using SupportDesk.Application;
using SupportDesk.Endpoints;
using SupportDesk.Hosting;
using SupportDesk.Infrastructure;
bool initialize = args.Contains("--init-lab");
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--init-lab").ToArray());
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);
builder.Services.AddAuthentication("Closed").AddScheme<AuthenticationSchemeOptions, ClosedGate>("Closed", _ => { });
builder.Services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(Environment.GetEnvironmentVariable("SUPPORTDESK_PG")
    ?? throw new InvalidOperationException("Set SUPPORTDESK_PG for the isolated database lab.")));
builder.Services.AddScoped<ITicketStore, PostgresTicketStore>();
builder.Services.AddScoped<Tickets>();
var app = builder.Build();
if (initialize)
{
    await using var scope = app.Services.CreateAsyncScope();
    await ((PostgresTicketStore)scope.ServiceProvider.GetRequiredService<ITicketStore>()).InitializeLabAsync(CancellationToken.None);
    Console.WriteLine("PASS: isolated testing schema initialized"); return;
}
app.UseExceptionHandler(handler => handler.Run(context => Results.Problem(statusCode: 500, title: "Storage operation failed.").ExecuteAsync(context)));
app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapTickets();
app.Run();
public partial class Program { }
