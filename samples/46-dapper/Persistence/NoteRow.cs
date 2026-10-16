namespace SupportDesk.Persistence;
public sealed class NoteRow
{
 public int Id { get; set; }
 public int TicketId { get; set; }
 public string Body { get; set; } = "";
 public TicketRow Ticket { get; set; } = null!;
}
