using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

internal sealed class InMemoryTicketStore : ITicketStore, ITicketQueries, ITicketLifecycle
{
    private long nextId;
    private readonly object gate = new();
    private readonly Dictionary<int, TicketSnapshot> rows = new();

    internal InMemoryTicketStore(IEnumerable<Ticket> seed)
    {
        foreach (var ticket in seed) rows.Add(ticket.Id, new(ticket, 1));
        nextId = rows.Count == 0 ? 1 : (long)rows.Keys.Max() + 1;
    }

    public Task<Ticket?> CreateAsync(TicketTitle title, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(title);
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            var data = rows;
            if (nextId > int.MaxValue) return Task.FromResult<Ticket?>(null);
            var created = Ticket.Create((int)nextId, title);
            data.Add(created.Id, new(created, 1));
            nextId++;
            
            return Task.FromResult<Ticket?>(created);
        }
    }
    public Task<SaveOutcome> DeleteAsync(TicketSnapshot expected, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(expected);
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            var data = rows;
            if (!data.TryGetValue(expected.Value.Id, out var current)) return Task.FromResult(SaveOutcome.Missing);
            if (current.Version != expected.Version) return Task.FromResult(SaveOutcome.Conflict);
            data.Remove(expected.Value.Id);
            
            return Task.FromResult(SaveOutcome.Saved);
        }
    }

    public Task<TicketResponse?> FindAsync(int id, CancellationToken ct)
    {
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            var data = rows;
            return Task.FromResult(data.TryGetValue(id, out var row) ? TicketMapping.ToResponse(row.Value) : null);
        }
    }
    public Task<TicketPage> ListAsync(TicketListQuery query, CancellationToken ct)
    {
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(QueryProjection.Page(rows.Values, query));
        }
    }

    public Task<TicketSnapshot?> LoadAsync(int id, CancellationToken ct)
    {
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            rows.TryGetValue(id, out var row);
            return Task.FromResult(row);
        }
    }

    public Task<SaveOutcome> SaveAsync(TicketSnapshot expected, Ticket changed, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(changed);
        if (changed.Id != expected.Value.Id)
            throw new ArgumentException("A mutation cannot change ticket identity.", nameof(changed));
        lock (gate)
        {
            // Cancellation before the commit point leaves storage untouched.
            ct.ThrowIfCancellationRequested();
            if (!rows.TryGetValue(changed.Id, out var current))
                return Task.FromResult(SaveOutcome.Missing);
            if (current.Version != expected.Version)
                return Task.FromResult(SaveOutcome.Conflict);
            rows[changed.Id] = new(changed, checked(current.Version + 1));
            return Task.FromResult(SaveOutcome.Saved);
        }
    }
}
