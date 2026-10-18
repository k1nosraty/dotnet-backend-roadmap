using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;
public sealed class MemoryTicketStore : ITicketStore
{
    private readonly Dictionary<Guid, TicketSnapshot> rows = [];
    private readonly object gate = new();
    public Task<TicketSnapshot> CreateAsync(TicketTitle title, string owner, CancellationToken ct)
    {
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            var row = new TicketSnapshot(Ticket.Open(Guid.NewGuid(), title), 1, owner);
            rows.Add(row.Value.Id, row); return Task.FromResult(row);
        }
    }
    public Task<TicketSnapshot?> LoadAsync(Guid id, CancellationToken ct)
    { lock (gate) { ct.ThrowIfCancellationRequested(); rows.TryGetValue(id, out var row); return Task.FromResult(row); } }
    public Task<SaveOutcome> SaveAsync(TicketSnapshot expected, Ticket changed, CancellationToken ct)
    {
        if (changed.Id != expected.Value.Id) throw new ArgumentException("Identity cannot change.", nameof(changed));
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            if (!rows.TryGetValue(changed.Id, out var row) || row.OwnerId != expected.OwnerId) return Task.FromResult(SaveOutcome.Missing);
            if (row.Version != expected.Version) return Task.FromResult(SaveOutcome.Conflict);
            rows[changed.Id] = new(changed, checked(row.Version + 1), row.OwnerId); return Task.FromResult(SaveOutcome.Saved);
        }
    }
}
