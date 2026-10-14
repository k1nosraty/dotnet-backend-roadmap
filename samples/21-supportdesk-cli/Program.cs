using SupportDesk.Application;
using SupportDesk.Infrastructure;
using SupportDesk.Presentation;

if (args.Length == 0) { Console.WriteLine("Usage: <data-path> [command]; no command opens the menu."); return 2; }
using CancellationTokenSource shutdown = new();
ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; shutdown.Cancel(); };
Console.CancelKeyPress += handler;
try
{
    JsonTicketStore store = new(args[0]);
    ConsoleCommands commands = new(new TicketService(store));
    string[] operation = args.Skip(1).ToArray();
    if (operation.Length == 1 && operation[0] == "--cancel-demo")
    {
        shutdown.Cancel();
        await store.SaveAsync(new TicketState(), shutdown.Token);
        return 0;
    }
    return operation.Length == 0 ? await InteractiveMenu.RunAsync(commands, shutdown.Token)
        : await commands.ExecuteAsync(operation, shutdown.Token);
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { Console.WriteLine("Cancelled."); return 130; }
catch (ArgumentException) { Console.Error.WriteLine("Invalid path or caller input."); return 1; }
finally { Console.CancelKeyPress -= handler; }
