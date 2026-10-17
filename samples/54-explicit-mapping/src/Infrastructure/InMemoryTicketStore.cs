using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

internal sealed class InMemoryTicketStore : ITicketStore
{
    private readonly object gate = new();
    private readonly Dictionary<int, TicketSnapshot> rows = new();

    internal InMemoryTicketStore(IEnumerable<Ticket> seed)
    {
        foreach (var ticket in seed) rows.Add(ticket.Id, new(ticket, 1));
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
