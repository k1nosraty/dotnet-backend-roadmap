using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;
public sealed class InMemoryTicketStore : ITicketStore
{
    private readonly object gate = new();
    private readonly Dictionary<int, Ticket> tickets = new();
    private long nextId = 1;
    public Ticket Create(TicketTitle title)
    {
        lock (gate)
        {
            if (nextId > int.MaxValue) throw new InvalidOperationException("ID capacity reached.");
            Ticket ticket = Ticket.Create((int)nextId, title);
            tickets.Add(ticket.Id, ticket); nextId++; return ticket;
        }
    }
    public Ticket? Find(int id) { lock (gate) { return tickets.GetValueOrDefault(id); } }
    public Ticket[] All() { lock (gate) { return tickets.Values.OrderBy(t => t.Id).ToArray(); } }
    public Ticket? Rename(int id, TicketTitle title)
    {
        lock (gate)
        {
            if (!tickets.TryGetValue(id, out Ticket? ticket)) return null;
            Ticket renamed = ticket.Rename(title); tickets[id] = renamed; return renamed;
        }
    }
    public bool Delete(int id) { lock (gate) { return tickets.Remove(id); } }
}
