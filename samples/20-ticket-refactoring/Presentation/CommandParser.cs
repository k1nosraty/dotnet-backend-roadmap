using System.Globalization;
using SupportDesk.Application;
namespace SupportDesk.Presentation;

public abstract record TicketCommand;
public sealed record AddTicket(string RawTitle) : TicketCommand;
public sealed record FindTicket(int Id) : TicketCommand;
public static class CommandParser
{
    public static Outcome<TicketCommand> Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length == 2 && args[0] == "add") return Outcome<TicketCommand>.Success(new AddTicket(args[1]));
        if (args.Length == 2 && args[0] == "find" &&
            int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out int id) && id > 0)
            return Outcome<TicketCommand>.Success(new FindTicket(id));
        return Outcome<TicketCommand>.Failure(ErrorCode.InvalidInput, "Usage: add <title> | find <positive-id>");
    }
}
