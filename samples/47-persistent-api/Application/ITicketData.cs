using SupportDesk.Domain;
using SupportDesk.Contracts;
namespace SupportDesk.Application;
public sealed record TicketSnapshot(Ticket Value, Guid Revision);
// No EF entities or IQueryable cross this boundary. Each operation is cancellable.
public interface ITicketData
{
 Task<Ticket> CreateAsync(TicketTitle title, CancellationToken ct);
 Task<TicketSnapshot?> LoadAsync(int id, CancellationToken ct);
 Task<MutationResult> SaveAsync(TicketSnapshot original, Ticket changed, CancellationToken ct);
 Task<MutationResult> DeleteAsync(TicketSnapshot original, CancellationToken ct);
 Task<PageResponse<TicketResponse>> ListAsync(string? q, int page, int size, CancellationToken ct);
}
