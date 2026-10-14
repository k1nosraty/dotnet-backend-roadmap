using SupportDesk.Application;
namespace SupportDesk.Presentation;

public sealed class ConsoleCommands
{
    private readonly TicketService service;
    public ConsoleCommands(TicketService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        this.service = service;
    }
    public int Execute(string[] args)
    {
        Outcome<TicketCommand> parsed = CommandParser.Parse(args);
        if (!parsed.IsSuccess) return Print(Outcome<TicketSummary>.Failure(parsed.Error!.Code, parsed.Error.Message));
        Outcome<TicketSummary> result = parsed.Value switch
        {
            AddTicket add => service.Register(add.RawTitle),
            FindTicket find => service.Find(find.Id),
            _ => throw new InvalidOperationException("Unhandled command type.")
        };
        return Print(result);
    }
    private static int Print(Outcome<TicketSummary> result)
    {
        if (result.IsSuccess) { Console.WriteLine($"#{result.Value.Id}: {result.Value.Title}"); return 0; }
        Console.Error.WriteLine($"{result.Error!.Code}: {result.Error.Message}");
        return result.Error.Code == ErrorCode.NotFound ? 3 : 2;
    }
}
