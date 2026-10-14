using SupportDesk.Domain;

try
{
    _ = new Ticket(1, "ab");
}
catch (ArgumentException)
{
    Console.WriteLine("Invalid title rejected.");
}
Ticket ticket = new(1, "Network issue");
try
{
    ticket.Close();
}
catch (InvalidOperationException)
{
    Console.WriteLine($"Invalid transition rejected; status: {ticket.Status}");
}
string path = args.Length > 0 ? args[0] : "activity.log";
try
{
    using (StreamWriter writer = new(path, append: true))
    {
        writer.WriteLine($"Viewed ticket #{ticket.Id}");
    }
    // The writer has been disposed before reopening the file.
    using StreamReader reader = new(path);
    Console.WriteLine($"Log: {reader.ReadLine()}");
}
catch (UnauthorizedAccessException)
{
    Console.Error.WriteLine("Access to log denied.");
    Environment.ExitCode = 1;
}
catch (IOException)
{
    Console.Error.WriteLine("Log could not be opened or written.");
    Environment.ExitCode = 1;
}
