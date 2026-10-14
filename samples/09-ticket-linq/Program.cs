using SupportDesk.Domain;

List<Ticket> tickets = new()
{
    new Ticket(3, "Email issue"),
    new Ticket(1, "Network issue"),
    new Ticket(2, "Printer issue")
};
tickets[1].Start();
tickets[1].Close();
IEnumerable<Ticket> deferredOpen = tickets.Where(t => t.Status == TicketStatus.Open);
List<Ticket> snapshotOpen = deferredOpen.ToList();
tickets.Add(new Ticket(4, "Backup issue"));
Console.WriteLine($"Deferred count: {deferredOpen.Count()}");
Console.WriteLine($"Snapshot count: {snapshotOpen.Count}");
foreach (string line in deferredOpen.OrderBy(t => t.Id).Select(t => $"#{t.Id}: {t.Title}"))
    Console.WriteLine(line);
Ticket? missing = tickets.FirstOrDefault(t => t.Id == 99);
Console.WriteLine(missing is null ? "Ticket not found." : missing.Title);
foreach (var group in tickets.GroupBy(t => t.Status).OrderBy(g => g.Key))
    Console.WriteLine($"{group.Key}: {group.Count()}");
