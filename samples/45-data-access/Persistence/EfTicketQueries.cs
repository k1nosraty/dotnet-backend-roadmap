using Microsoft.EntityFrameworkCore;
using SupportDesk.Application;
namespace SupportDesk.Persistence;
public sealed class EfTicketQueries(DeskContext db) : ITicketQueries
{
 public Task<TicketSummary[]> ListAsync(CancellationToken ct) => db.Tickets.AsNoTracking()
  .OrderBy(t => t.Id).Take(20).Select(t => new TicketSummary(t.Id, t.Title, t.Notes.Count)).ToArrayAsync(ct);
}
