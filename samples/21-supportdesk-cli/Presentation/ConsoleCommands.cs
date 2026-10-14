using System.Globalization;
using System.Text.Json;
using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Presentation;

public sealed class ConsoleCommands
{
    private readonly TicketService service;
    public ConsoleCommands(TicketService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        this.service = service;
    }
    public async Task<int> ExecuteAsync(string[] args, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            if (args.Length == 1 && args[0] == "help") { Help(); return 0; }
            if (args.Length == 1 && args[0] == "report")
            {
                TicketReport report = await service.ReportAsync(token);
                Console.WriteLine($"Open: {report.Open}\nInProgress: {report.InProgress}\nClosed: {report.Closed}"); return 0;
            }
            if ((args.Length == 1 || (args.Length == 2 && args[1] == "--open")) && args[0] == "list")
            {
                TicketList list = await service.ListAsync(args.Length == 2, token);
                foreach (TicketSummary ticket in list.Items) PrintTicket(ticket);
                if (list.Items.Count == 0) Console.WriteLine("No tickets."); return 0;
            }
            if (args.Length is 2 or 3 && args[0] == "add")
            {
                TicketPriority priority = TicketPriority.Normal;
                if (args.Length == 3 && !(args[2] is "Low" or "Normal" or "High")) return Usage();
                if (args.Length == 3) priority = Enum.Parse<TicketPriority>(args[2]);
                return Print(await service.AddAsync(args[1], priority, token), "Saved");
            }
            if (args.Length is 2 or 3 && int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out int id) && id > 0)
            {
                Outcome<TicketSummary>? result = args[0] switch
                {
                    "find" when args.Length == 2 => await service.FindAsync(id, token),
                    "rename" when args.Length == 3 => await service.RenameAsync(id, args[2], token),
                    "note" when args.Length == 3 => await service.NoteAsync(id, args[2], token),
                    "start" when args.Length == 2 => await service.StartAsync(id, token),
                    "close" when args.Length == 2 => await service.CloseAsync(id, token),
                    "delete" when args.Length == 2 => await service.DeleteAsync(id, token),
                    _ => null
                };
                if (result is not null) return Print(result, args[0] == "find" ? "Found" : args[0] == "delete" ? "Deleted" : "Saved");
            }
            return Usage();
        }
        catch (JsonException) { return Fail("Invalid JSON; file retained."); }
        catch (InvalidDataException) { return Fail("Invalid stored data; file retained."); }
        catch (UnauthorizedAccessException) { return Fail("Storage access denied."); }
        catch (IOException) { return Fail("Storage failed; no success reported."); }
    }
    private static int Print(Outcome<TicketSummary> result, string prefix)
    {
        if (result.IsSuccess) { Console.Write($"{prefix} "); PrintTicket(result.Value); return 0; }
        Console.Error.WriteLine($"{result.Error!.Code}: {result.Error.Message}");
        return result.Error.Code == ErrorCode.NotFound ? 3 : 2;
    }
    private static void PrintTicket(TicketSummary ticket)
    {
        Console.WriteLine($"#{ticket.Id}: {ticket.Title} [{ticket.Priority}] ({ticket.Status}); notes={ticket.Notes.Count}");
        foreach (string note in ticket.Notes) Console.WriteLine($"  - {note}");
    }
    private static int Fail(string message) { Console.Error.WriteLine(message); return 1; }
    private static int Usage() { Help(); return 2; }
    public static void Help() => Console.WriteLine("add <title> [Low|Normal|High] | list [--open] | find <id> | rename <id> <title> | note <id> <text> | start <id> | close <id> | delete <id> | report | help");
}
