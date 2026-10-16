using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SupportDesk.Infrastructure;
using SupportDesk.Persistence;
return await Lab.RunAsync("lab47", args, async (source, ct) =>
{
 var options = new DbContextOptionsBuilder<DeskContext>().UseNpgsql(source).Options;
 await using var db = new DeskContext(options);
 // Lab owns a new EMPTY schema. Never call CreateTables against an existing application schema.
 await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(ct);
 db.Add(new TicketRow { Title = "Robert'); DROP TABLE tickets;--", Notes = [new NoteRow { Body = "Parameterized" }] });
 db.Add(new TicketRow { Title = "Other" }); await db.SaveChangesAsync(ct);
 await using var connection = await source.OpenConnectionAsync(ct);
 var query = new Dapper.CommandDefinition("""
  SELECT t."Id" AS "Id", t."Title" AS "Title", count(n."Id")::int AS "NoteCount"
  FROM tickets t LEFT JOIN notes n ON n."TicketId" = t."Id"
  WHERE t."Title" = @Title GROUP BY t."Id", t."Title" ORDER BY t."Id" LIMIT @Limit
  """, new { Title = "Robert'); DROP TABLE tickets;--", Limit = 10 }, commandTimeout: 20, cancellationToken: ct);
 var rows = (await Dapper.SqlMapper.QueryAsync<TicketSummary>(connection, query)).ToArray();
 Lab.Check(rows.Length == 1 && rows[0].NoteCount == 1, "Parameterized read and left join count");
 Lab.Check(await db.Tickets.CountAsync(ct) == 2, "Input remained data");
 Console.WriteLine("PASS: Dapper parameterized report");
});
