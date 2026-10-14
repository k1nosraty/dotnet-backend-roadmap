using System.Globalization;
using System.Text.Json;
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
    public int Execute(string[] arguments)
    {
        try
        {
            if (arguments.Length == 2 && arguments[0] == "add")
            {
                TicketSummary ticket = service.Register(arguments[1]);
                Console.WriteLine($"Saved #{ticket.Id}: {ticket.Title}");
                return 0;
            }
            if (arguments.Length == 2 && arguments[0] == "find" &&
                int.TryParse(arguments[1], NumberStyles.None, CultureInfo.InvariantCulture, out int id) && id > 0)
            {
                TicketSummary? ticket = service.Find(id);
                Console.WriteLine(ticket is null ? "Ticket not found." : $"Found #{ticket.Id}: {ticket.Title}");
                return 0;
            }
            Console.Error.WriteLine("Usage: <path> add <title> | <path> find <positive-id>");
            return 2;
        }
        catch (JsonException) { return Fail("Invalid JSON; file retained."); }
        catch (InvalidDataException) { return Fail("Invalid stored data; file retained."); }
        catch (UnauthorizedAccessException) { return Fail("Storage access denied."); }
        catch (IOException) { return Fail("Storage failed; no success reported."); }
        catch (ArgumentException) { return Fail("Invalid title or input."); }
    }
    private static int Fail(string message) { Console.Error.WriteLine(message); return 1; }
}
