using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Persistence;
namespace SupportDesk.Accounts;

public static class AccountServices
{
    public static void AddAccounts(IServiceCollection services, Action<DbContextOptionsBuilder> database)
    {
        services.AddDbContext<AccountContext>(database);
        services.AddAuthorization();
        services.AddIdentityApiEndpoints<IdentityUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;
            options.Password.RequiredLength = 12;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
        }).AddEntityFrameworkStores<AccountContext>();
        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromMinutes(15));
    }
}
