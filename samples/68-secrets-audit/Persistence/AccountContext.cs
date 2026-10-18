using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace SupportDesk.Persistence;

public sealed class AccountContext(DbContextOptions<AccountContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<SupportDesk.Tickets.OwnedTicket> Tickets => Set<SupportDesk.Tickets.OwnedTicket>();
    public DbSet<SupportDesk.Audit.AuditEntry> AuditEntries => Set<SupportDesk.Audit.AuditEntry>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Retain Identity indexes, keys and concurrency stamps.
        builder.Entity<SupportDesk.Audit.AuditEntry>(a =>
        {
            a.HasKey(x => x.Id);
            a.Property(x => x.ActorId).HasMaxLength(450).IsRequired();
            a.Property(x => x.TenantId).HasMaxLength(80).IsRequired();
            a.Property(x => x.Kind).HasConversion<string>().HasMaxLength(40).IsRequired();
            a.HasIndex(x => new { x.TenantId, x.OccurredAt });
        });
        builder.Entity<SupportDesk.Tickets.OwnedTicket>(t =>
        {
            t.HasKey(x => x.Id);
            t.Property(x => x.OwnerId).HasMaxLength(450).IsRequired();
            t.Property(x => x.TenantId).HasMaxLength(80).IsRequired();
            t.Property(x => x.Title).HasMaxLength(120).IsRequired();
            t.HasIndex(x => new { x.TenantId, x.OwnerId });
            t.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
