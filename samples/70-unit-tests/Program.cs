using SupportDesk.Domain;
var opened = Ticket.Open(Guid.NewGuid(), TicketTitle.Create("Network issue"));
var closed = opened.Start().Resolve("Cable replaced");
Console.WriteLine($"{closed.Status}: {closed.Resolution}; original={opened.Status}");
