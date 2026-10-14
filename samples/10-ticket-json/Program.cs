using System.Text.Json;
using SupportDesk.Domain;
using SupportDesk.Storage;

string path = args.Length > 0 ? args[0] : "tickets.json";
string operation = args.Length > 1 ? args[1] : "list";
try
{
    JsonTicketStore store = new(path);
    List<Ticket> tickets = store.Load();
    if (operation == "add")
    {
        string title = args.Length > 2 ? args[2] : "Network issue";
        int maximum = tickets.Count == 0 ? 0 : tickets.Max(t => t.Id);
        if (maximum == int.MaxValue) throw new InvalidDataException("ID capacity reached.");
        Ticket ticket = new(maximum + 1, title);
        tickets.Add(ticket);
        store.Save(tickets);
        Console.WriteLine($"Saved #{ticket.Id}");
    }
    else if (operation != "list")
    {
        Console.Error.WriteLine("Usage: <path> [list|add] [title]");
        Environment.ExitCode = 2;
        return;
    }
    foreach (Ticket ticket in tickets.OrderBy(t => t.Id))
        Console.WriteLine($"#{ticket.Id}: {ticket.Title} ({ticket.Status})");
    Console.WriteLine($"Count: {tickets.Count}");
}
catch (JsonException) { Fail("Invalid JSON; original file retained."); }
catch (InvalidDataException) { Fail("Invalid stored data; original file retained."); }
catch (ArgumentException) { Fail("Invalid input; original file retained."); }
catch (UnauthorizedAccessException) { Fail("Access denied; check file permissions."); }
catch (IOException) { Fail("File operation failed; inspect path and storage."); }

static void Fail(string message)
{
    Console.Error.WriteLine(message);
    Environment.ExitCode = 1;
}
