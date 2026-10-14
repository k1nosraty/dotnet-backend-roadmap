using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

public sealed class InMemoryTicketStore : ITicketReader, ITicketWriter
{
    private readonly Dictionary<int, Ticket> tickets = new();
    private long nextId = 1;
    public int Add(string title)
    {
        if (nextId > int.MaxValue) throw new InvalidOperationException("ID capacity reached.");
        Ticket ticket = new((int)nextId, title);
        tickets.Add(ticket.Id, ticket);
        nextId++;
        return ticket.Id;
    }
    public Ticket? FindById(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        return tickets.TryGetValue(id, out Ticket? ticket) ? ticket : null;
    }
}
