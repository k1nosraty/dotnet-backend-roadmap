using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SupportDesk.Infrastructure;
using SupportDesk.Persistence;
return await Lab.RunAsync("lab46", args, async (source, ct) =>
{
 var options = new DbContextOptionsBuilder<DeskContext>().UseNpgsql(source).Options;
 await using var db = new DeskContext(options);
 // Lab owns a new EMPTY schema. Never call CreateTables against an existing application schema.
 await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(ct);
 db.Add(new TicketRow { Title = "Boundary", Notes = [new NoteRow { Body = "Visible note" }] });
 await db.SaveChangesAsync(ct); db.ChangeTracker.Clear();
 SupportDesk.Application.ITicketQueries port = new SupportDesk.Persistence.EfTicketQueries(db);
 var service = new SupportDesk.Application.TicketReport(port);
 var report = await service.RunAsync(ct);
 Lab.Check(report.Length == 1 && report[0].NoteCount == 1, "Query port contract");
 Lab.Check(!db.ChangeTracker.Entries().Any(), "Read boundary does not track entities");
 Console.WriteLine("PASS: use-case query port and DTO boundary");
});
