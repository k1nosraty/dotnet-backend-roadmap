using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SupportDesk.Infrastructure;
using SupportDesk.Persistence;
return await Lab.RunAsync("lab44", args, async (source, ct) =>
{
 var options = new DbContextOptionsBuilder<DeskContext>().UseNpgsql(source).Options;
 await using var db = new DeskContext(options);
 // Lab owns a new EMPTY schema. Never call CreateTables against an existing application schema.
 await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(ct);
 db.AddRange(Enumerable.Range(1, 3).Select(i => new TicketRow { Title = $"Ticket {i}", Notes = [new NoteRow { Body = "Note" }] }));
 await db.SaveChangesAsync(ct); db.ChangeTracker.Clear();
 var counter = new CommandCounter();
 await using var read = new DeskContext(new DbContextOptionsBuilder<DeskContext>().UseNpgsql(source).AddInterceptors(counter).Options);
 // Deliberate N+1: explicit extra query per parent; no lazy-loading package is installed.
 var parents = await read.Tickets.AsNoTracking().OrderBy(t => t.Id).ToListAsync(ct);
 foreach (var ticket in parents) _ = await read.Notes.CountAsync(n => n.TicketId == ticket.Id, ct);
 Lab.Check(counter.Reads == 4, "N+1 count");
 counter.Reads = 0;
 var query = read.Tickets.AsNoTracking().OrderBy(t => t.Id)
  .Select(t => new TicketSummary(t.Id, t.Title, t.Notes.Count));
 Console.WriteLine(query.ToQueryString());
 var summaries = await query.ToArrayAsync(ct);
 Lab.Check(counter.Reads == 1 && summaries.All(t => t.NoteCount == 1), "One server projection");
 Lab.Check(!read.ChangeTracker.Entries().Any(), "DTO projection tracks no entities");
 var detached = await read.Tickets.AsNoTracking().OrderBy(t => t.Id).FirstAsync(ct);
 detached.Title = "Not persisted";
 Lab.Check(await read.SaveChangesAsync(ct) == 0, "Detached mutation is not saved");
 Console.WriteLine("PASS: N+1=4, projection=1, no tracked DTOs");
});
