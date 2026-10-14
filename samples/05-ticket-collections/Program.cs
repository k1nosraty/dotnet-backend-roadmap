using System.Globalization;

Dictionary<int, string> titlesById = new();
List<string> activity = new();
int nextId = 1;

while (true)
{
    Console.WriteLine("1. Add | 2. List | 3. Find | 4. Delete | 5. Activity | 0. Exit");
    string? choice = Console.ReadLine();
    if (choice is null || choice.Trim() == "0")
    {
        break;
    }
    switch (choice.Trim())
    {
        case "1":
        {
            string? title = ReadTitle();
            if (title is null) return;
            if (nextId == int.MaxValue)
            {
                Console.WriteLine("ID capacity reached.");
                break;
            }
            int id = nextId;
            titlesById.Add(id, title);
            nextId++;
            activity.Add($"Added #{id}");
            Console.WriteLine($"Added #{id}: {title}");
            break;
        }
        case "2":
            if (titlesById.Count == 0) Console.WriteLine("No tickets.");
            foreach (KeyValuePair<int, string> ticket in titlesById)
            {
                Console.WriteLine($"#{ticket.Key}: {ticket.Value}");
            }
            break;
        case "3":
        case "4":
        {
            Console.Write("Positive ID: ");
            string? rawId = Console.ReadLine();
            if (rawId is null) return;
            if (!int.TryParse(rawId.Trim(), NumberStyles.None,
                CultureInfo.InvariantCulture, out int id) || id <= 0)
            {
                Console.WriteLine("Invalid ID.");
                break;
            }
            if (choice.Trim() == "3")
            {
                if (titlesById.TryGetValue(id, out string? title))
                    Console.WriteLine($"Found #{id}: {title}");
                else
                    Console.WriteLine("Ticket not found.");
            }
            else
            {
                if (titlesById.Remove(id))
                {
                    activity.Add($"Deleted #{id}");
                    Console.WriteLine($"Deleted #{id}");
                }
                else Console.WriteLine("Ticket not found.");
            }
            break;
        }
        case "5":
            if (activity.Count == 0) Console.WriteLine("No activity.");
            foreach (string entry in activity) Console.WriteLine(entry);
            break;
        default:
            Console.WriteLine("Unknown option.");
            break;
    }
}
Console.WriteLine("Goodbye.");

static string? ReadTitle()
{
    while (true)
    {
        Console.Write("Title (3-120 characters): ");
        string? raw = Console.ReadLine();
        if (raw is null) return null;
        string title = raw.Trim();
        if (title.Length >= 3 && title.Length <= 120) return title;
        Console.WriteLine("Invalid title. Try again.");
    }
}
