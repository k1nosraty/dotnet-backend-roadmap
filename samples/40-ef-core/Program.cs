using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SupportDesk.Infrastructure;
using SupportDesk.Persistence;
return await Lab.RunAsync("lab41", args, async (source, ct) =>
{
 var options = new DbContextOptionsBuilder<DeskContext>().UseNpgsql(source).Options;
 await using var db = new DeskContext(options);
 // Lab owns a new EMPTY schema. Never call CreateTables against an existing application schema.
 await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(ct);
 var row = new TicketRow { Title = "Database connection" };
 db.Tickets.Add(row);
 Lab.Check(db.Entry(row).State == EntityState.Added, "Added before saving");
 await db.SaveChangesAsync(ct);
 Lab.Check(row.Id > 0 && db.Entry(row).State == EntityState.Unchanged, "Generated identity");
 row.Title = "Database connected";
 db.ChangeTracker.DetectChanges();
 Lab.Check(db.Entry(row).State == EntityState.Modified, "Detect mutation");
 await db.SaveChangesAsync(ct);
 await using var fresh = new DeskContext(options);
 Lab.Check(await fresh.Tickets.AsNoTracking().AnyAsync(t => t.Id == row.Id && t.Title == row.Title, ct), "Read through new context");
 Console.WriteLine("PASS: EF identity and unit of work");
});
