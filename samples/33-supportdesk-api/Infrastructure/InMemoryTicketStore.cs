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
    public MutationResult Rename(int id, TicketTitle title) => Mutate(id, ticket => ticket.Rename(title));
    public MutationResult Mutate(int id, Func<Ticket, Ticket?> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        lock (gate)
        {
            if (!tickets.TryGetValue(id, out Ticket? ticket)) return MutationResult.Missing();
            Ticket? changed = change(ticket);
            if (changed is null) return MutationResult.Conflict();
            if (changed.Id != id) throw new InvalidOperationException("Mutation cannot change ticket identity.");
            tickets[id] = changed; return MutationResult.Success(changed);
        }
    }
    public bool Delete(int id) { lock (gate) { return tickets.Remove(id); } }
}
