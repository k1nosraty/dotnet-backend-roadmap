using System.Security.Claims;
using IdentityLab;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
var builder=WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication",LogLevel.None);
builder.Logging.AddFilter("Microsoft.IdentityModel",LogLevel.None);
builder.Services.AddAntiforgery(o=>{o.HeaderName="X-CSRF-TOKEN";o.Cookie.SecurePolicy=CookieSecurePolicy.Always;});
builder.Services.AddAuthentication(o=>{o.DefaultScheme="Session";o.DefaultChallengeScheme="Session";})
    .AddCookie("Session",o=>
    {
        o.Cookie.Name="__Host-CourseOidc";o.Cookie.HttpOnly=true;o.Cookie.SecurePolicy=CookieSecurePolicy.Always;
        o.Cookie.SameSite=SameSiteMode.Lax;o.ExpireTimeSpan=TimeSpan.FromMinutes(20);o.SlidingExpiration=false;
        o.Events.OnRedirectToLogin=context=>{context.Response.StatusCode=401;return Task.CompletedTask;};
        o.Events.OnRedirectToAccessDenied=context=>{context.Response.StatusCode=403;return Task.CompletedTask;};
    }).AddOpenIdConnect("oidc",_=>{});
builder.Services.AddOptions<OpenIdConnectOptions>("oidc").Configure<IConfiguration>((o,c)=>
{
    o.Authority=Settings.Authority(c);o.ClientId=Settings.Required(c,"Oidc:ClientId");o.ClientSecret=Settings.Required(c,"Oidc:ClientSecret");
    o.SignInScheme="Session";o.ResponseType=OpenIdConnectResponseType.Code;o.ResponseMode=OpenIdConnectResponseMode.Query;
    o.UsePkce=true;o.RequireHttpsMetadata=true;o.MapInboundClaims=false;o.SaveTokens=false;
    o.Scope.Clear();o.Scope.Add("openid");o.Scope.Add("profile");
    o.CallbackPath="/signin-oidc";o.GetClaimsFromUserInfoEndpoint=false;
    o.TokenValidationParameters=new TokenValidationParameters {ValidateIssuer=true,ValidIssuer=o.Authority,
        ValidateAudience=true,ValidAudience=o.ClientId,ValidateLifetime=true,RequireExpirationTime=true,
        RequireSignedTokens=true,ValidateIssuerSigningKey=true,ValidAlgorithms=[SecurityAlgorithms.RsaSha256],
        ClockSkew=TimeSpan.FromSeconds(5),NameClaimType="name",RoleClaimType="roles",IncludeTokenOnFailedValidation=false};
    // Keep protocol defaults: state, correlation and nonce validation remain enabled.
    o.Events.OnRemoteFailure=context=>{context.HandleResponse();context.Response.StatusCode=400;return context.Response.WriteAsync("External sign-in failed.");};
}).ValidateOnStart();
builder.Services.AddAuthorization(o=>o.FallbackPolicy=new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
var app=builder.Build();
app.Use(async(context,next)=>
{
    context.Response.Headers.CacheControl="no-store";
    if(!context.Request.IsHttps){context.Response.StatusCode=400;return;}
    await next();
});
app.UseAuthentication();app.UseAuthorization();
app.MapGet("/health",()=>Results.Text("Healthy")).AllowAnonymous();
app.MapGet("/login",()=>Results.Challenge(new AuthenticationProperties {RedirectUri="/me"},["oidc"])).AllowAnonymous();
app.MapGet("/me",(ClaimsPrincipal user)=>Results.Ok(new {subject=user.FindFirstValue("sub"),name=user.Identity?.Name}));
app.MapGet("/csrf",(HttpContext context,IAntiforgery antiforgery)=>Results.Ok(new {token=antiforgery.GetAndStoreTokens(context).RequestToken}));
app.MapPost("/logout",async(HttpContext context,IAntiforgery antiforgery)=>
{
    try{await antiforgery.ValidateRequestAsync(context);}
    catch(AntiforgeryValidationException){return Results.BadRequest();}
    await context.SignOutAsync("Session");return Results.NoContent();
});
app.Run();
public partial class Program { }
