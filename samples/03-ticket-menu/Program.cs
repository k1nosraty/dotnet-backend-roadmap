int registeredTickets = 0;
bool running = true;

while (running)
{
    Console.WriteLine("1. Register title | 2. Show count | 0. Exit");
    string? choice = Console.ReadLine();
    if (choice is null)
    {
        Console.WriteLine("Input ended.");
        break;
    }

    switch (choice.Trim())
    {
        case "1":
        {
            while (true)
            {
                Console.Write("Title (3-120 characters): ");
                string? rawTitle = Console.ReadLine();
                if (rawTitle is null)
                {
                    running = false;
                    break;
                }
                string title = rawTitle.Trim();
                if (title.Length < 3 || title.Length > 120)
                {
                    Console.WriteLine("Invalid title. Try again.");
                    continue;
                }
                registeredTickets++;
                Console.WriteLine($"Registered: {title}");
                break;
            }
            break;
        }
        case "2":
            Console.WriteLine($"Registered count: {registeredTickets}");
            break;
        case "0":
            running = false;
            break;
        default:
            Console.WriteLine("Unknown option.");
            break;
    }
}
Console.WriteLine("Goodbye.");
