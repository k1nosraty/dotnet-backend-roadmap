using System.Globalization;

string teamName = "IT Support";
int openTickets = 8;
int closedTickets = 2;
int totalTickets = openTickets + closedTickets;

decimal completionPercentage = totalTickets == 0
    ? 0m
    : (decimal)closedTickets / totalTickets * 100m;

Console.WriteLine($"Team: {teamName}");
Console.WriteLine($"Open: {openTickets}");
Console.WriteLine($"Closed: {closedTickets}");
Console.WriteLine($"Total: {totalTickets}");
Console.WriteLine($"Completion: {completionPercentage.ToString("0.0", CultureInfo.InvariantCulture)}%");
