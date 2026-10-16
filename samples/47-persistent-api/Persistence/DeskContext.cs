using Microsoft.EntityFrameworkCore;
namespace SupportDesk.Persistence;
public sealed class DeskContext(DbContextOptions<DeskContext> options) : DbContext(options)
{
 public DbSet<TicketRow> Tickets => Set<TicketRow>();
 public DbSet<NoteRow> Notes => Set<NoteRow>();
 protected override void OnModelCreating(ModelBuilder model)
 {
  var b = model.Entity<TicketRow>();
  b.ToTable("tickets", t =>
  {
   t.HasCheckConstraint("ck_title", "char_length(\"Title\") BETWEEN 1 AND 120 AND \"Title\" = btrim(\"Title\")");
   t.HasCheckConstraint("ck_status", "\"Status\" IN ('Open','InProgress','Closed')");
   t.HasCheckConstraint("ck_ticket_id", "\"Id\" > 0");
  }); b.HasKey(x => x.Id);
  b.Property(x => x.Title).IsRequired().HasMaxLength(120);
  b.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Open");
  b.HasIndex(x => new { x.Status, x.Id });
  
  b.Property(x => x.Revision).IsConcurrencyToken();
  var note = model.Entity<NoteRow>();
  note.ToTable("notes", t => t.HasCheckConstraint("ck_body", "char_length(\"Body\") BETWEEN 1 AND 500 AND \"Body\" = btrim(\"Body\")")); note.HasKey(x => x.Id);
  note.Property(x => x.Body).IsRequired().UseCollation("C");
  note.HasIndex(x => new { x.TicketId, x.Body }).IsUnique();
  note.HasOne(x => x.Ticket).WithMany(x => x.Notes).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Restrict);
 }
}
