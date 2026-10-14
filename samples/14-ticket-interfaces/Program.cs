using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

if (args.Contains("--failure"))
{
    try { Console.WriteLine(new TicketSummaryService(new FailingTicketReader()).GetSummary(1)); }
    catch (IOException) { Console.Error.WriteLine("Reader failed; this is not a missing ticket."); Environment.ExitCode = 1; }
    return;
}
List<Ticket> seed = new() { new(1, "Network issue") };
ITicketReader memory = new InMemoryTicketReader(seed);
TicketSummaryService first = new(memory);
Console.WriteLine(first.GetSummary(1));
Console.WriteLine(first.GetSummary(99));
TicketSummaryService second = new(new FixedTicketReader());
Console.WriteLine(second.GetSummary(10));
Console.WriteLine(second.GetSummary(1));
Console.WriteLine($"Memory retained: {first.GetSummary(1)}");
