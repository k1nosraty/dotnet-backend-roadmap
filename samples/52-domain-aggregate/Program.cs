using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

if (args.Contains("--check"))
{
    await Verification.RunAsync();
    AggregateChecks.Run();
    return;
}
var ticket = Ticket.Create(1, TicketTitle.Create("شبکه ساختمان"))
    .ChangeStatus("InProgress")!;
var store = StoreFactory.Create([ticket]);
var result = await new ResolveTicket(store).ExecuteAsync(new(1, "اتصال بررسی و اصلاح شد"), CancellationToken.None);
Console.WriteLine($"{result.Code}: {result.Ticket?.Id} {result.Ticket?.Status} notes={result.Ticket?.NoteCount}");
