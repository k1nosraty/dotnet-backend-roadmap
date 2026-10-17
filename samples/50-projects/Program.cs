using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

if (args.Contains("--check"))
{
    await Verification.RunAsync();
    return;
}
var ticket = Ticket.Create(1, TicketTitle.Create("شبکه ساختمان"))
    .ChangeStatus("InProgress")!.AddNote("اتصال بررسی و اصلاح شد")!;
var store = StoreFactory.Create([ticket]);
var result = await new CloseTicket(store).ExecuteAsync(1, CancellationToken.None);
Console.WriteLine($"{result.Code}: {result.Ticket?.Id} {result.Ticket?.Status} notes={result.Ticket?.NoteCount}");
