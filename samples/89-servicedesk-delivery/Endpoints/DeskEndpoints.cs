using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Persistence;
namespace SupportDesk.Endpoints;
public sealed record CreateInput(string? Title,string? Priority);
public sealed record ChangeInput(int Version,string? Resolution=null);
public sealed record LoginInput(string? Email,string? Password);
public static class DeskEndpoints
{
    public static async Task<Actor?> ActorAsync(ClaimsPrincipal principal,SignInManager<DeskUser> signIn,TimeProvider clock)
    {
        var user=await signIn.ValidateSecurityStampAsync(principal);
        if(user is null||!user.EmailConfirmed||user.Tenant==Guid.Empty||user.LockoutEnd>clock.GetUtcNow())return null;
        return new(user.Id,user.Tenant,user.IsAgent);
    }
    public static void MapDesk(this WebApplication app)
    {
        app.MapPost("/auth/login",async (LoginInput input,SignInManager<DeskUser> signIn)=>
        {
            if(string.IsNullOrWhiteSpace(input.Email)||input.Email.Length>254||input.Password is null||input.Password.Length>1024)return Results.Unauthorized();
            signIn.AuthenticationScheme=IdentityConstants.BearerScheme;
            var result=await signIn.PasswordSignInAsync(input.Email,input.Password,false,true);
            return result.Succeeded?Results.Empty:Results.Unauthorized();
        }).AllowAnonymous().RequireRateLimiting("login");
        var api=app.MapGroup("/requests").RequireAuthorization().RequireRateLimiting("api");
        api.MapPost("/",async(CreateInput input,HttpContext context,SignInManager<DeskUser> signIn,TimeProvider clock,DeskService desk,CancellationToken ct)=>
        {
            var actor=await ActorAsync(context.User,signIn,clock);if(actor is null)return Results.Unauthorized();
            if(!Guid.TryParse(context.Request.Headers["Idempotency-Key"],out var key))return Results.BadRequest();
            var result=await desk.CreateAsync(actor,key,input.Title,input.Priority,ct);return Results.Json(result.View,statusCode:result.Status);
        });
        api.MapGet("/",async(int? page,int? size,ClaimsPrincipal user,SignInManager<DeskUser> signIn,TimeProvider clock,DeskService desk,CancellationToken ct)=>
        {
            if(page is <1 or >10000 || size is <1 or >50)return Results.BadRequest();
            var actor=await ActorAsync(user,signIn,clock);if(actor is null)return Results.Unauthorized();
            return Results.Ok(await desk.ListAsync(actor,page??1,size??20,ct));
        });
        api.MapGet("/{id:guid}",async(Guid id,ClaimsPrincipal user,SignInManager<DeskUser> signIn,TimeProvider clock,DeskService desk,CancellationToken ct)=>
        {
            var actor=await ActorAsync(user,signIn,clock);if(actor is null)return Results.Unauthorized();
            var request=await desk.ReadAsync(actor,id,ct);return request is null?Results.NotFound():Results.Ok(RequestView.From(request));
        });
        api.MapGet("/{id:guid}/audit",async(Guid id,ClaimsPrincipal user,SignInManager<DeskUser> signIn,TimeProvider clock,DeskService desk,CancellationToken ct)=>
        {
            var actor=await ActorAsync(user,signIn,clock);if(actor is null)return Results.Unauthorized();
            var result=await desk.AuditAsync(actor,id,ct);return result is null?Results.NotFound():Results.Ok(result);
        });
        foreach(var action in new[]{"claim","close","reopen"})
            api.MapPost("/{id:guid}/"+action,async(Guid id,ChangeInput input,ClaimsPrincipal user,SignInManager<DeskUser> signIn,TimeProvider clock,DeskService desk,CancellationToken ct)=>
            {
                var actor=await ActorAsync(user,signIn,clock);if(actor is null)return Results.Unauthorized();
                if(input.Version<1)return Results.BadRequest();
                var result=await desk.TransitionAsync(actor,id,input.Version,action,input.Resolution,ct);return Results.Json(result.View,statusCode:result.Status);
            });
    }
}
