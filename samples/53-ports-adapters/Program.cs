using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

if (args.Contains("--check"))
{
    await Verification.RunAsync();
    AggregateChecks.Run();
    await AdapterChecks.RunAsync();
    return;
}
var ticket = Ticket.Create(1, TicketTitle.Create("شبکه ساختمان"))
    .ChangeStatus("InProgress")!;
var store = args.Length == 2 && args[0] == "--file"
    ? StoreFactory.CreateFile(args[1], [ticket])
    : StoreFactory.Create([ticket]);
var result = await new ResolveTicket(store).ExecuteAsync(new(1, "اتصال بررسی و اصلاح شد"), CancellationToken.None);
Console.WriteLine($"{result.Code}: {result.Ticket?.Id} {result.Ticket?.Status} notes={result.Ticket?.NoteCount}");

var persisted = (await store.LoadAsync(1, CancellationToken.None))!;
Console.WriteLine($"Stored: {persisted.Value.Status} notes={persisted.Value.Notes.Count} version={persisted.Version}");
