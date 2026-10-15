using SupportDesk.Domain;
using SupportDesk.Contracts;
namespace SupportDesk.Infrastructure;

// Thread-safe for this in-process demo; data is lost on restart.
public sealed class TicketStore
{
    private readonly object gate = new();
    private readonly Dictionary<int, TicketResponse> tickets = new();
    private long nextId = 1;
    public TicketResponse Create(TicketTitle title)
    {
        ArgumentNullException.ThrowIfNull(title);
        lock (gate)
        {
            if (nextId > int.MaxValue) throw new InvalidOperationException("ID capacity reached.");
            TicketResponse ticket = new((int)nextId, title.Value);
            tickets.Add(ticket.Id, ticket); nextId++;
            return ticket;
        }
    }
    public TicketResponse? Find(int id)
    {
        lock (gate) { return tickets.TryGetValue(id, out TicketResponse? ticket) ? ticket : null; }
    }
    public TicketResponse[] All()
    {
        lock (gate) { return tickets.Values.OrderBy(t => t.Id).ToArray(); }
    }
    public bool Delete(int id)
    {
        lock (gate) { return tickets.Remove(id); }
    }
}
