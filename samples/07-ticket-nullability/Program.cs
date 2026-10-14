using SupportDesk.Domain;

Dictionary<int, Ticket> tickets = new() { [1] = new Ticket(1, "Network issue") };
Ticket? found = FindById(tickets, 1);
if (found is not null) Console.WriteLine($"Found: {found.Title}");
Ticket? missing = FindById(tickets, 99);
Console.WriteLine($"Missing: {missing?.Title ?? "Ticket not found"}");
int? selectedId = null;
Console.WriteLine($"Selection: {selectedId?.ToString() ?? "None"}");

static Ticket? FindById(Dictionary<int, Ticket> tickets, int id)
{
    return tickets.TryGetValue(id, out Ticket? ticket) ? ticket : null;
}
