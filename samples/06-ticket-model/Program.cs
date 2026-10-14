using SupportDesk.Domain;

Ticket first = new(1, "  Network issue  ");
Ticket sameReference = first;
sameReference.Rename("Router issue");
first.Start();
first.Close();
Ticket second = new(2, "Printer issue");
Console.WriteLine($"First: #{first.Id} {first.Title} {first.Status}");
Console.WriteLine($"Alias is first: {ReferenceEquals(first, sameReference)}");
Console.WriteLine($"Second: #{second.Id} {second.Title} {second.Status}");
