using Microsoft.EntityFrameworkCore;
namespace SupportDesk.Persistence;
public sealed class DeskContext(DbContextOptions<DeskContext> options) : DbContext(options)
{
 public DbSet<TicketRow> Tickets => Set<TicketRow>();
 
 protected override void OnModelCreating(ModelBuilder model)
 {
  var b = model.Entity<TicketRow>();
  b.ToTable("tickets"); b.HasKey(x => x.Id);
  b.Property(x => x.Title).IsRequired().HasMaxLength(120);
  b.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Open");
  b.HasIndex(x => new { x.Status, x.Id });
  b.Property(x => x.Priority).IsRequired().HasMaxLength(20).HasDefaultValue("Normal"); // EVOLVE_PRIORITY
  
  
 }
}
