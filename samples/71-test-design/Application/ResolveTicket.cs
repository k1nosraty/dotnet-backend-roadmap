using SupportDesk.Domain;
namespace SupportDesk.Application;
public sealed record TicketSnapshot(Ticket Value, long Version);
public enum SaveOutcome { Saved, Missing, Conflict }
public enum ResolveOutcome { Resolved, Invalid, Missing, Conflict }
public interface ITicketStore
{
    Task<TicketSnapshot?> LoadAsync(Guid id, CancellationToken ct);
    Task<SaveOutcome> SaveAsync(TicketSnapshot expected, Ticket changed, CancellationToken ct);
}
public sealed class ResolveTicket(ITicketStore store)
{
    public async Task<ResolveOutcome> ExecuteAsync(Guid id, string? resolution, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string text = resolution?.Trim() ?? "";
        if (id == Guid.Empty || !TextRules.HasLength(text, 200)) return ResolveOutcome.Invalid;
        var snapshot = await store.LoadAsync(id, ct);
        if (snapshot is null) return ResolveOutcome.Missing;
        if (snapshot.Value.Status != TicketStatus.InProgress) return ResolveOutcome.Conflict;
        var changed = snapshot.Value.Resolve(text);
        return await store.SaveAsync(snapshot, changed, ct) switch
        {
            SaveOutcome.Saved => ResolveOutcome.Resolved,
            SaveOutcome.Missing => ResolveOutcome.Missing,
            _ => ResolveOutcome.Conflict
        };
    }
}
