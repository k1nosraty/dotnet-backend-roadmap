namespace SupportDesk.Presentation;

public static class InteractiveMenu
{
    public static async Task<int> RunAsync(ConsoleCommands commands, CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            Console.WriteLine("1 Add | 2 List | 3 Find | 4 Rename | 5 Note | 6 Start | 7 Close | 8 Delete | 9 Report | 0 Exit");
            string? choice = Console.ReadLine();
            token.ThrowIfCancellationRequested();
            if (choice is null || choice.Trim() == "0") return 0;
            string command = choice.Trim() switch
            {
                "1" => "add", "2" => "list", "3" => "find", "4" => "rename", "5" => "note",
                "6" => "start", "7" => "close", "8" => "delete", "9" => "report", _ => "help"
            };
            List<string> args = new() { command };
            if (command is "find" or "rename" or "note" or "start" or "close" or "delete")
            {
                Console.Write("ID: "); string? id = Console.ReadLine(); if (id is null) return 0; args.Add(id.Trim());
            }
            if (command is "add" or "rename" or "note")
            {
                Console.Write("Text: "); string? text = Console.ReadLine(); if (text is null) return 0; args.Add(text);
            }
            if (command == "add")
            {
                Console.Write("Priority (Low/Normal/High; empty=Normal): ");
                string? priority = Console.ReadLine(); if (priority is null) return 0;
                args.Add(string.IsNullOrWhiteSpace(priority) ? "Normal" : priority.Trim());
            }
            int code = await commands.ExecuteAsync(args.ToArray(), token);
            if (code == 1) return code; // Stop after storage failure; do not continue silently.
        }
    }
}
