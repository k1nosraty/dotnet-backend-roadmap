using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Reporting;
using SupportDesk.Routing;

Ticket ticket = new(1, "Network issue");
ticket.AddNote("First investigation");
ticket.AddNote("Second investigation");
TicketReviewService strict = new(new TicketReviewPolicy(2));
TicketReviewService relaxed = new(new TicketReviewPolicy(3));
Console.WriteLine($"Threshold 2: {strict.GetDecision(ticket)}");
Console.WriteLine($"Threshold 3: {relaxed.GetDecision(ticket)}");
List<TicketReport> reports = new() { new PlainTextTicketReport(), new DetailedTicketReport() };
foreach (TicketReport report in reports) Console.WriteLine(report.Generate(ticket));
Console.WriteLine($"Unchanged: {ticket.Status}; notes={ticket.Notes.Count}");
ticket.Start();
ticket.Close();
Console.WriteLine($"Closed: {strict.GetDecision(ticket)}");
