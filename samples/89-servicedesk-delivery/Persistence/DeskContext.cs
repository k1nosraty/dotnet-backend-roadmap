using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain;
namespace SupportDesk.Persistence;
public sealed class DeskUser:IdentityUser
{
    public Guid Tenant {get;set;}
    public bool IsAgent {get;set;}
}
public sealed class DeskAudit
{
    public Guid Id {get;set;}=Guid.NewGuid();
    public Guid RequestId {get;set;}
    public Guid Tenant {get;set;}
    public string ActorId {get;set;}="";
    public string Kind {get;set;}="";
    public DateTimeOffset At {get;set;}
}
public sealed class DeskContext(DbContextOptions<DeskContext> options):IdentityDbContext<DeskUser>(options)
{
    public DbSet<DeskRequest> Requests=>Set<DeskRequest>();
    public DbSet<DeskAudit> Audits=>Set<DeskAudit>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<DeskRequest>(row=>
        {
            row.HasKey(x=>x.Id); row.Property(x=>x.Owner).HasMaxLength(450);
            row.Property(x=>x.Title).HasMaxLength(120);row.Property(x=>x.Priority).HasMaxLength(10);
            row.Property(x=>x.State).HasConversion<string>().HasMaxLength(20);
            row.Property(x=>x.Version).IsConcurrencyToken();row.Property(x=>x.Resolution).HasMaxLength(500);
            row.Property(x=>x.AssignedTo).HasMaxLength(450);
            row.HasIndex(x=>new {x.Tenant,x.Owner,x.Key}).IsUnique().HasDatabaseName("ux_request_key");
            row.HasIndex(x=>new {x.Tenant,x.CreatedAt,x.Id});
            row.HasOne<DeskUser>().WithMany().HasForeignKey(x=>x.Owner).OnDelete(DeleteBehavior.Restrict);
            row.HasOne<DeskUser>().WithMany().HasForeignKey(x=>x.AssignedTo).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<DeskAudit>(row=>
        {
            row.HasKey(x=>x.Id);row.Property(x=>x.ActorId).HasMaxLength(450);row.Property(x=>x.Kind).HasMaxLength(20);
            row.HasOne<DeskRequest>().WithMany().HasForeignKey(x=>x.RequestId).OnDelete(DeleteBehavior.Restrict);
            row.HasIndex(x=>new {x.Tenant,x.RequestId,x.At});
        });
    }
}
