using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Hosting;
var builder=WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o=>o.SerializerOptions.UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow);
builder.Services.AddAuthentication("Closed").AddScheme<AuthenticationSchemeOptions,ClosedGate>("Closed",_=>{});
builder.Services.AddAuthorization(o=>o.FallbackPolicy=new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddSingleton<MemoryDesk>();
var app=builder.Build();
app.UseAuthentication();app.UseAuthorization();
app.MapGet("/health/live",()=>Results.Text("Healthy")).AllowAnonymous();
Actor? ActorOf(ClaimsPrincipal user)=>Guid.TryParse(user.FindFirstValue("tenant"),out var tenant)&&user.FindFirstValue(ClaimTypes.NameIdentifier) is string id
    ?new(id,tenant,user.IsInRole("Agent")):null;
app.MapPost("/requests",(CreateInput input,HttpContext context,MemoryDesk desk)=>
{
    var actor=ActorOf(context.User);if(actor is null)return Results.Forbid();
    if(!Guid.TryParse(context.Request.Headers["Idempotency-Key"],out var key)||key==Guid.Empty ||!DeskRequest.ValidText(input.Title,120)||input.Priority is not("normal" or "urgent"))return Results.BadRequest();
    var result=desk.Create(actor,key,input.Title,input.Priority);return Results.Json(result.View,statusCode:result.Status);
});
app.MapGet("/requests/{id:guid}",(Guid id,ClaimsPrincipal user,MemoryDesk desk)=>
    ActorOf(user) is Actor actor && desk.Read(actor,id) is RequestView view?Results.Ok(view):Results.NotFound());
app.MapPost("/requests/{id:guid}/claim",(Guid id,VersionInput input,ClaimsPrincipal user,MemoryDesk desk)=>
{
    if(ActorOf(user) is not Actor actor)return Results.Forbid();
    var result=desk.Claim(actor,id,input.Version);return Results.Json(result.View,statusCode:result.Status);
});
app.Run();
public sealed record CreateInput(string? Title,string? Priority);
public sealed record VersionInput(int Version);
public partial class Program { }
