using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

public sealed class InMemoryTicketReader : ITicketReader
{
    private readonly Dictionary<int, Ticket> tickets;
    public InMemoryTicketReader(IEnumerable<Ticket> tickets)
    {
        ArgumentNullException.ThrowIfNull(tickets);
        this.tickets = tickets.ToDictionary(t => t.Id);
    }
    public Ticket? FindById(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        return tickets.TryGetValue(id, out Ticket? ticket) ? ticket : null;
    }
}
