using Microsoft.EntityFrameworkCore;
namespace SupportDesk.Persistence;
public sealed class DeskContext(DbContextOptions<DeskContext> options) : DbContext(options)
{
 public DbSet<TicketRow> Tickets => Set<TicketRow>();
 public DbSet<NoteRow> Notes => Set<NoteRow>();
 protected override void OnModelCreating(ModelBuilder model)
 {
  var b = model.Entity<TicketRow>();
  b.ToTable("tickets"); b.HasKey(x => x.Id);
  b.Property(x => x.Title).IsRequired().HasMaxLength(120);
  b.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Open");
  b.HasIndex(x => new { x.Status, x.Id });
  
  b.Property(x => x.Revision).IsConcurrencyToken();
  var note = model.Entity<NoteRow>();
  note.ToTable("notes"); note.HasKey(x => x.Id);
  note.Property(x => x.Body).IsRequired().UseCollation("C");
  note.HasIndex(x => new { x.TicketId, x.Body }).IsUnique();
  note.HasOne(x => x.Ticket).WithMany(x => x.Notes).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Restrict);
 }
}
