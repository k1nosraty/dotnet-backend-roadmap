using SupportDesk.Domain;
namespace SupportDesk.Application;

// Ticket is immutable. Version is internal concurrency state, never an HTTP input.
public sealed record TicketSnapshot(Ticket Value, long Version);
public enum SaveOutcome { Saved, Missing, Conflict }

public interface ITicketStore
{
    Task<TicketSnapshot?> LoadAsync(int id, CancellationToken ct);
    // One atomic compare-and-swap of an entire aggregate, not one Save per field.
    Task<SaveOutcome> SaveAsync(TicketSnapshot expected, Ticket changed, CancellationToken ct);
}
