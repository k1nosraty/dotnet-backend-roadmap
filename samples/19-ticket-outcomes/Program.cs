using SupportDesk.Application;
using SupportDesk.Infrastructure;

InMemoryTicketStore store = new();
TicketService service = new(args.Contains("--failure") ? new FailingWriter() : store, store);
try
{
    Print(service.Register("ab"));
    Print(service.Register("Network issue"));
    Print(service.Find(99));
    Print(service.Find(0));
}
catch (IOException) { Console.Error.WriteLine("Persistence failed; no success reported."); Environment.ExitCode = 1; }

static void Print(Outcome<TicketSummary> outcome)
{
    if (outcome.IsSuccess) Console.WriteLine($"Success #{outcome.Value.Id}: {outcome.Value.Title}");
    else Console.WriteLine($"Error {outcome.Error!.Code}: {outcome.Error.Message}");
}
