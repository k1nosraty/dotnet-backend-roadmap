using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SupportDesk.Application;
using SupportDesk.Contracts;
using SupportDesk.Domain;
namespace SupportDesk.Persistence;
public sealed class EfTicketData(DeskContext db) : ITicketData
{
 public async Task<Ticket> CreateAsync(TicketTitle title, CancellationToken ct)
 {
  var row = new TicketRow { Title = title.Value };
  db.Tickets.Add(row); await db.SaveChangesAsync(ct);
  return Ticket.Create(row.Id, title);
 }
 public async Task<TicketSnapshot?> LoadAsync(int id, CancellationToken ct)
 {
  // One SELECT gives parent revision and children from one statement snapshot.
  var row = await db.Tickets.Include(t => t.Notes).AsSingleQuery().SingleOrDefaultAsync(t => t.Id == id, ct);
  return row is null ? null : new TicketSnapshot(Hydrate(row), row.Revision);
 }
 private static Ticket Hydrate(TicketRow row)
 {
  var ticket = Ticket.Create(row.Id, TicketTitle.Create(row.Title));
  foreach (var note in row.Notes.OrderBy(n => n.Id))
   ticket = ticket.AddNote(note.Body) ?? throw new InvalidOperationException("Invalid stored notes.");
  if (row.Status != "Open")
  {
   ticket = ticket.ChangeStatus("InProgress") ?? throw new InvalidOperationException("Invalid transition.");
   if (row.Status == "Closed") ticket = ticket.ChangeStatus("Closed") ?? throw new InvalidOperationException("Invalid stored closed ticket.");
   else if (row.Status != "InProgress") throw new InvalidOperationException("Invalid stored status.");
  }
  return ticket;
 }
 private TicketRow Tracked(TicketSnapshot original)
 {
  var row = db.Tickets.Local.Single(t => t.Id == original.Value.Id);
  if (row.Revision != original.Revision) throw new InvalidOperationException("Snapshot mismatch.");
  return row;
 }
 public async Task<MutationResult> SaveAsync(TicketSnapshot original, Ticket changed, CancellationToken ct)
 {
  if (changed.Id != original.Value.Id) throw new InvalidOperationException("Aggregate identity changed.");
  var row = Tracked(original);
  row.Title = changed.Title; row.Status = changed.Status;
  foreach (var text in changed.Notes.Skip(original.Value.Notes.Count)) row.Notes.Add(new NoteRow { Body = text });
  // Every aggregate mutation, including adding a child, changes the parent token.
  row.Revision = Guid.NewGuid();
  try { await db.SaveChangesAsync(ct); return MutationResult.Success(changed); }
  catch (DbUpdateConcurrencyException) { return MutationResult.Conflict(); }
  catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
  { return MutationResult.Conflict(); }
 }
 public async Task<MutationResult> DeleteAsync(TicketSnapshot original, CancellationToken ct)
 {
  var row = Tracked(original);
  // RESTRICT remains the database default. Explicit deletion is one atomic SaveChanges.
  db.Notes.RemoveRange(row.Notes); db.Tickets.Remove(row);
  try { await db.SaveChangesAsync(ct); return MutationResult.Success(original.Value); }
  catch (DbUpdateConcurrencyException) { return MutationResult.Conflict(); }
  catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23001" or "23503" })
  { return MutationResult.Conflict(); }
 }
 public async Task<PageResponse<TicketResponse>> ListAsync(string? q, int page, int size, CancellationToken ct)
 {
  // Count and rows share one PostgreSQL snapshot even during concurrent inserts.
  await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
  var query = db.Tickets.AsNoTracking();
  if (!string.IsNullOrWhiteSpace(q)) query = query.Where(t => t.Title.Contains(q.Trim()));
  int count = await query.CountAsync(ct);
  long skip = ((long)page - 1) * size;
  TicketResponse[] rows = skip >= count ? [] : await query.OrderBy(t => t.Id).Skip((int)skip).Take(size)
   .Select(t => new TicketResponse(t.Id, t.Title, t.Status,
    t.Notes.OrderBy(n => n.Id).Select(n => n.Body).ToArray())).ToArrayAsync(ct);
  await tx.CommitAsync(ct);
  return new(rows, page, size, count);
 }
}
