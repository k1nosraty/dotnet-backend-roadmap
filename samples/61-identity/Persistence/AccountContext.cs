using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace SupportDesk.Persistence;

public sealed class AccountContext(DbContextOptions<AccountContext> options) : IdentityDbContext<IdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Retain Identity indexes, keys and concurrency stamps.
    }
}
