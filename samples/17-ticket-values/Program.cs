using SupportDesk.Domain;

TicketTitle first = TicketTitle.Create("  Network issue  ");
TicketTitle second = TicketTitle.Create("Network issue");
Console.WriteLine($"Same value: {first == second}; same reference: {ReferenceEquals(first, second)}");
Console.WriteLine($"Case sensitive: {first == TicketTitle.Create("network issue")}");
HashSet<TicketTitle> titles = new() { first, second };
Console.WriteLine($"Unique values: {titles.Count}");
TicketSummary original = new(1, first.Value);
TicketSummary changed = original with { Title = "Printer issue" };
Console.WriteLine($"Original: {original.Title}; copy: {changed.Title}");
try { _ = TicketTitle.Create("ab"); }
catch (ArgumentException) { Console.WriteLine("Invalid value rejected."); }
ReportLines report = new(new List<string> { "First line" });
ReportLines shallow = report with { };
shallow.Lines.Add("Shared second line");
Console.WriteLine($"Shallow original count: {report.Lines.Count}");
Console.WriteLine($"List content equality automatic: {new ReportLines(new List<string> { "same" }) == new ReportLines(new List<string> { "same" })}");

public sealed record TicketSummary(int Id, string Title);
public sealed record ReportLines(List<string> Lines);
