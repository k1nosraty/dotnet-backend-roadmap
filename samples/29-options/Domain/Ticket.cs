namespace SupportDesk.Domain;
public sealed record Ticket
{
    public int Id { get; }
    public string Title { get; }
    private Ticket(int id, string title) { Id = id; Title = title; }
    public static Ticket Create(int id, TicketTitle title)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        ArgumentNullException.ThrowIfNull(title);
        return new(id, title.Value);
    }
    public Ticket Rename(TicketTitle title)
    {
        ArgumentNullException.ThrowIfNull(title);
        return new(Id, title.Value);
    }
}
