int registeredTickets = 0;

while (true)
{
    ShowMenu();
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
            if (title is null)
            {
                Console.WriteLine("Input ended.");
                return;
            }
            registeredTickets++;
            Console.WriteLine($"Registered: {title}");
            break;
        }
        case "2":
            PrintCount(registeredTickets);
            break;
        default:
            Console.WriteLine("Unknown option.");
            break;
    }
}
Console.WriteLine("Goodbye.");

static void ShowMenu()
{
    Console.WriteLine("1. Register title | 2. Show count | 0. Exit");
}

static bool IsValidTitle(string title)
{
    return title.Length >= 3 && title.Length <= 120;
}

static string? ReadTitle()
{
    while (true)
    {
        Console.Write("Title (3-120 characters): ");
        string? rawTitle = Console.ReadLine();
        if (rawTitle is null)
        {
            return null;
        }
        string title = rawTitle.Trim();
        if (IsValidTitle(title))
        {
            return title;
        }
        Console.WriteLine("Invalid title. Try again.");
    }
}

static void PrintCount(int count)
{
    Console.WriteLine($"Registered count: {count}");
}
