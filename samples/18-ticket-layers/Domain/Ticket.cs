namespace SupportDesk.Domain;

// This registration-only model deliberately has no status transitions yet.
public sealed class Ticket
{
    public int Id { get; }
    public TicketTitle Title { get; }
    public Ticket(int id, TicketTitle title)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        ArgumentNullException.ThrowIfNull(title);
        Id = id;
        Title = title;
    }
}
