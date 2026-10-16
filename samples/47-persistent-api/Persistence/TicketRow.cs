namespace SupportDesk.Persistence;
// Mutable persistence state; this is not the HTTP contract or the business model.
public sealed class TicketRow
{
 public int Id { get; set; }
 public string Title { get; set; } = "";
 public string Status { get; set; } = "Open";
 
 public Guid Revision { get; set; } = Guid.NewGuid();
 public List<NoteRow> Notes { get; set; } = [];
}
