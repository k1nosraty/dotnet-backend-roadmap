using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SupportDesk.Infrastructure;
using SupportDesk.Persistence;
return await Lab.RunAsync("lab43", args, async (source, ct) =>
{
 var options = new DbContextOptionsBuilder<DeskContext>().UseNpgsql(source).Options;
 await using var db = new DeskContext(options);
 // Lab owns a new EMPTY schema. Never call CreateTables against an existing application schema.
 await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(ct);
 var ticket = new TicketRow { Title = "Relationship", Notes = [new NoteRow { Body = "First note" }] };
 db.Add(ticket); await db.SaveChangesAsync(ct);
 db.ChangeTracker.Clear();
 var loaded = await db.Tickets.Include(t => t.Notes).SingleAsync(ct);
 Lab.Check(loaded.Notes.Count == 1 && loaded.Notes[0].TicketId == loaded.Id, "FK and navigation fixup");
 // New context loads only parent: RESTRICT is enforced by the real database.
 await using (var deleting = new DeskContext(options))
 {
  deleting.Remove(await deleting.Tickets.SingleAsync(ct));
  try { await deleting.SaveChangesAsync(ct); throw new InvalidOperationException("Expected FK rejection"); }
  catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23001" or "23503" })
  { Console.WriteLine("PASS: database restricts parent delete"); }
 }
 db.Notes.RemoveRange(loaded.Notes); await db.SaveChangesAsync(ct);
 db.Remove(loaded); await db.SaveChangesAsync(ct);
 Lab.Check(!await db.Tickets.AnyAsync(ct), "Explicit child-first deletion");
});
