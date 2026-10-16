using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Migrations;
using SupportDesk.Infrastructure;
using SupportDesk.Persistence;
return await Lab.RunAsync("lab42", args, async (source, ct) =>
{
 var options = new DbContextOptionsBuilder<DeskContext>().UseNpgsql(source).Options;
 await using var db = new DeskContext(options);
 // Upgrade an existing V1 database with a real row; not only an empty database.
 var migrator = db.GetService<IMigrator>();
 string initial = db.Database.GetMigrations().Single(x => x.EndsWith("InitialCreate", StringComparison.Ordinal));
 await migrator.MigrateAsync(initial, ct);
 await db.Database.ExecuteSqlRawAsync("INSERT INTO tickets (\"Title\") VALUES ('Legacy ticket')", ct);
 await db.Database.MigrateAsync(ct);
 Lab.Check(!db.Database.HasPendingModelChanges(), "Snapshot/model drift");
 var row = await db.Tickets.AsNoTracking().SingleAsync(ct);
 Lab.Check(row.Title == "Legacy ticket" && row.Priority == "Normal", "Existing data preserved/backfilled");
 await db.Database.MigrateAsync(ct);
 Lab.Check((await db.Database.GetAppliedMigrationsAsync(ct)).Count() == 2, "Repeat apply is a no-op");
 Console.WriteLine("PASS: upgrade preserved existing data and migration history");
});
