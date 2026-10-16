using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SupportDesk.Infrastructure;
using SupportDesk.Persistence;
return await Lab.RunAsync("lab45", args, async (source, ct) =>
{
 var options = new DbContextOptionsBuilder<DeskContext>().UseNpgsql(source).Options;
 await using var db = new DeskContext(options);
 // Lab owns a new EMPTY schema. Never call CreateTables against an existing application schema.
 await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(ct);
 db.Add(new TicketRow { Title = "Original" }); await db.SaveChangesAsync(ct);
 await using var first = new DeskContext(options);
 await using var second = new DeskContext(options);
 var a = await first.Tickets.SingleAsync(ct);
 var b = await second.Tickets.SingleAsync(ct);
 a.Title = "First writer"; a.Revision = Guid.NewGuid(); await first.SaveChangesAsync(ct);
 b.Title = "Lost update"; b.Revision = Guid.NewGuid();
 b.Notes.Add(new NoteRow { Body = "Must roll back" });
 try { await second.SaveChangesAsync(ct); throw new InvalidOperationException("Expected stale write rejection"); }
 catch (DbUpdateConcurrencyException) { Console.WriteLine("PASS: stale writer rejected"); }
 await using var verify = new DeskContext(options);
 Lab.Check((await verify.Tickets.SingleAsync(ct)).Title == "First writer", "Winner preserved");
 Lab.Check(!await verify.Notes.AnyAsync(ct), "Child insert rolled back with parent conflict");
 Console.WriteLine("PASS: atomic aggregate conflict");
});
