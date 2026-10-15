using SupportDesk.Infrastructure;
using SupportDesk.Data;
using SupportDesk.Domain;
return await Lab.RunAsync("lab36", args, async (db, token) =>
{
    await Lab.ScriptAsync(db, "01-schema.sql", token);
    var repository = new TicketRepository(db);
    var first = await repository.CreateAsync(TicketTitle.Create("  مشکل شبکه  "), token);
    Lab.Check(first.Title == "مشکل شبکه", "Unicode or normalization changed.");
    var injection = await repository.CreateAsync(TicketTitle.Create("Robert'); DROP TABLE tickets;--"), token);
    Lab.Check((await repository.FindAsync(injection.Id, token))?.Title == injection.Title, "Text was not stored as a value.");
    Lab.Check(await repository.RenameAsync(first.Id, TicketTitle.Create("Printer issue"), token), "Update failed.");
    Lab.Check((await repository.FindAsync(first.Id, token))?.Title == "Printer issue", "Read after update failed.");
    Lab.Check(!await repository.RenameAsync(int.MaxValue, TicketTitle.Create("Missing ticket"), token), "Missing update succeeded.");
    Lab.Check(await repository.DeleteAsync(first.Id, token), "Delete failed.");
    Lab.Check(!await repository.DeleteAsync(first.Id, token), "Repeated delete succeeded.");
    Lab.Check(await repository.FindAsync(first.Id, token) is null, "Deleted row survived.");
    Lab.Check(await repository.FindAsync(injection.Id, token) is not null, "Another row disappeared.");
    using var cancelled = new CancellationTokenSource();
    cancelled.Cancel();
    try { await repository.FindAsync(injection.Id, cancelled.Token); throw new InvalidOperationException("Cancellation ignored."); }
    catch (OperationCanceledException) { }
    Console.WriteLine("PASS: parameterized CRUD, Unicode, literal injection text, missing IDs and cancellation.");
});
