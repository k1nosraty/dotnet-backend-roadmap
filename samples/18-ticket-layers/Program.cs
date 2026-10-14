using SupportDesk.Application;
using SupportDesk.Infrastructure;
using SupportDesk.Presentation;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: <path> add <title> | <path> find <positive-id>");
    return 2;
}
try
{
    JsonTicketStore store = new(args[0]);
    TicketService service = new(store, store);
    ConsoleCommands commands = new(service);
    return commands.Execute(args.Skip(1).ToArray());
}
catch (ArgumentException)
{
    Console.Error.WriteLine("Invalid storage path.");
    return 1;
}
