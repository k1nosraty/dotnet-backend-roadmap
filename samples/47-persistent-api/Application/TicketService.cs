using SupportDesk.Domain;
namespace SupportDesk.Application;
public sealed class TicketService(ITicketData data, RequestStamp stamp)
{
 public Guid RequestId => stamp.Id;
 public Task<Ticket> CreateAsync(string title, CancellationToken ct) => data.CreateAsync(TicketTitle.Create(title), ct);
 public async Task<Ticket?> FindAsync(int id, CancellationToken ct) => (await data.LoadAsync(id, ct))?.Value;
 public Task<Contracts.PageResponse<Contracts.TicketResponse>> AllAsync(string? q, int page, int size, CancellationToken ct)
  => data.ListAsync(q, page, size, ct);
 public Task<MutationResult> RenameAsync(int id, string title, CancellationToken ct)
  => ChangeAsync(id, t => t.Rename(TicketTitle.Create(title)), ct);
 public Task<MutationResult> AddNoteAsync(int id, string text, CancellationToken ct)
  => ChangeAsync(id, t => t.AddNote(text), ct);
 public Task<MutationResult> ChangeStatusAsync(int id, string status, CancellationToken ct)
  => ChangeAsync(id, t => t.ChangeStatus(status), ct);
 private async Task<MutationResult> ChangeAsync(int id, Func<Ticket, Ticket?> change, CancellationToken ct)
 {
  var original = await data.LoadAsync(id, ct);
  if (original is null) return MutationResult.Missing();
  var changed = change(original.Value);
  if (changed is null) return MutationResult.Conflict();
  return await data.SaveAsync(original, changed, ct);
 }
 public async Task<MutationResult> DeleteAsync(int id, CancellationToken ct)
 {
  var original = await data.LoadAsync(id, ct);
  return original is null ? MutationResult.Missing() : await data.DeleteAsync(original, ct);
 }
}
