using System.Globalization;

Console.Write("Title (3-120 characters): ");
string? rawTitle = Console.ReadLine();
string title = rawTitle?.Trim() ?? string.Empty;

if (title.Length < 3 || title.Length > 120)
{
    Console.WriteLine("Invalid title.");
    return;
}

Console.Write("Estimated hours (0 < hours <= 1000; use a decimal point): ");
string? rawHours = Console.ReadLine();
bool parsed = decimal.TryParse(
    rawHours,
    NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
    CultureInfo.InvariantCulture,
    out decimal estimatedHours);

if (!parsed || estimatedHours <= 0m || estimatedHours > 1000m)
{
    Console.WriteLine("Invalid estimated hours.");
    return;
}

Console.WriteLine($"Ticket: {title}");
Console.WriteLine($"Estimate: {estimatedHours.ToString("0.##", CultureInfo.InvariantCulture)} hours");
