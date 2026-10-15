namespace SupportDesk.Domain;
public sealed record Ticket
{
    public int Id { get; }
    public string Title { get; }
    private Ticket(int id, string title) { Id = id; Title = title; }
    public static Ticket Create(int id, TicketTitle title) => new(id, title.Value);
    public Ticket Rename(TicketTitle title) => new(Id, title.Value);
}
