using System.Globalization;
using System.Text.Json;
using SupportDesk.Domain;
using SupportDesk.Storage;

using CancellationTokenSource shutdown = new();
ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; shutdown.Cancel(); };
Console.CancelKeyPress += handler;
try
{
    string path = args.Length > 0 ? args[0] : "tickets.json";
    JsonTicketStore store = new(path);
    CancellationToken token = shutdown.Token;
    if (args.Length > 1 && args[1] == "--cancel-demo")
    {
        shutdown.Cancel();
        await store.SaveAsync(new List<Ticket> { new(1, "Never saved") }, token);
        return;
    }
    List<Ticket> tickets = await store.LoadAsync(token);
    int maximum = tickets.Count == 0 ? 0 : tickets.Max(t => t.Id);
    long nextId = (long)maximum + 1;
    while (true)
    {
        token.ThrowIfCancellationRequested();
        Console.WriteLine("1. Add | 2. List | 3. Start | 4. Close | 5. Report | 0. Exit");
        string? choice = Console.ReadLine();
        token.ThrowIfCancellationRequested();
        if (choice is null || choice.Trim() == "0") break;
        switch (choice.Trim())
        {
            case "1":
            {
                Console.Write("Title: ");
                string? raw = Console.ReadLine();
                if (raw is null) return;
                string title = raw.Trim();
                if (title.Length < 3 || title.Length > 120)
                {
                    Console.WriteLine("Invalid title.");
                    break;
                }
                if (nextId > int.MaxValue)
                {
                    Console.WriteLine("ID capacity reached.");
                    break;
                }
                Ticket ticket = new((int)nextId, title);
                tickets.Add(ticket);
                await store.SaveAsync(tickets, token);
                nextId++;
                Console.WriteLine($"Saved #{ticket.Id}");
                break;
            }
            case "2":
                foreach (Ticket ticket in tickets.OrderBy(t => t.Id))
                    Console.WriteLine($"#{ticket.Id}: {ticket.Title} ({ticket.Status})");
                if (tickets.Count == 0) Console.WriteLine("No tickets.");
                break;
            case "3":
            case "4":
            {
                Console.Write("ID: ");
                string? raw = Console.ReadLine();
                if (raw is null) return;
                if (!int.TryParse(raw.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int id) || id <= 0)
                {
                    Console.WriteLine("Invalid ID.");
                    break;
                }
                Ticket? ticket = tickets.FirstOrDefault(t => t.Id == id);
                if (ticket is null) { Console.WriteLine("Ticket not found."); break; }
                if ((choice.Trim() == "3" && ticket.Status != TicketStatus.Open) ||
                    (choice.Trim() == "4" && ticket.Status != TicketStatus.InProgress))
                {
                    Console.WriteLine("Invalid transition.");
                    break;
                }
                if (choice.Trim() == "3") ticket.Start(); else ticket.Close();
                await store.SaveAsync(tickets, token);
                Console.WriteLine($"Saved status: {ticket.Status}");
                break;
            }
            case "5":
                foreach (TicketStatus status in Enum.GetValues<TicketStatus>())
                    Console.WriteLine($"{status}: {tickets.Count(t => t.Status == status)}");
                break;
            default: Console.WriteLine("Unknown option."); break;
        }
    }
    Console.WriteLine("Goodbye.");
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
{
    Console.WriteLine("Cancelled.");
}
catch (JsonException) { Fail("Invalid JSON; original file retained."); }
catch (InvalidDataException) { Fail("Invalid stored data; original file retained."); }
catch (ArgumentException) { Fail("Invalid path or input."); }
catch (UnauthorizedAccessException) { Fail("Access denied."); }
catch (IOException) { Fail("Storage failed; program stopped."); }
finally { Console.CancelKeyPress -= handler; }

static void Fail(string message)
{
    Console.Error.WriteLine(message);
    Environment.ExitCode = 1;
}
