using System.Security.Claims;
using IdentityLab;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
var builder=WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication",LogLevel.None);
builder.Logging.AddFilter("Microsoft.IdentityModel",LogLevel.None);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IConfiguration>((options,config)=>
{
    options.Authority=Settings.Authority(config);
    options.Audience=Settings.Required(config,"Tokens:Audience");
    options.RequireHttpsMetadata=true;options.MapInboundClaims=false;options.IncludeErrorDetails=false;
    options.TokenValidationParameters=new TokenValidationParameters {
        ValidateIssuer=true,ValidIssuer=options.Authority,ValidateAudience=true,ValidAudience=options.Audience,
        ValidateLifetime=true,RequireExpirationTime=true,RequireSignedTokens=true,ValidateIssuerSigningKey=true,
        ValidAlgorithms=[SecurityAlgorithms.RsaSha256],ValidTypes=["at+jwt"],ClockSkew=TimeSpan.FromSeconds(5),
        NameClaimType="sub",RoleClaimType="roles",IncludeTokenOnFailedValidation=false
    };
    options.Events=new JwtBearerEvents {OnTokenValidated=context=>
    {
        if(string.IsNullOrWhiteSpace(context.Principal?.FindFirstValue("sub")))context.Fail("Subject is required.");
        return Task.CompletedTask;
    }};
}).ValidateOnStart();
builder.Services.AddAuthorization(options=>
{
    options.FallbackPolicy=new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("ReadTickets",policy=>policy.RequireAuthenticatedUser().RequireAssertion(context=>
        context.User.FindAll("scope").SelectMany(c=>c.Value.Split(' ',StringSplitOptions.RemoveEmptyEntries)).Contains("tickets.read",StringComparer.Ordinal)));
});
var app=builder.Build();
app.Use(async(context,next)=>
{
    context.Response.Headers.CacheControl="no-store";
    if(!context.Request.IsHttps){context.Response.StatusCode=400;return;}
    await next();
});
app.UseAuthentication();app.UseAuthorization();
app.MapGet("/health",()=>Results.Text("Healthy")).AllowAnonymous();
app.MapGet("/api/me",(ClaimsPrincipal user)=>Results.Ok(new {subject=user.FindFirstValue("sub")}));
app.MapGet("/api/tickets",()=>Results.Ok(new[]{new {id=1,title="Synthetic ticket"}})).RequireAuthorization("ReadTickets");
app.Run();
public partial class Program { }
