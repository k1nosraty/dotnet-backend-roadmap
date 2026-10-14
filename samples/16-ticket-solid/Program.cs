using SupportDesk.Application;
using SupportDesk.Infrastructure;
using SupportDesk.Presentation;

InMemoryTicketStore store = new();
TicketRegistrationService registration = new(store);
int first = registration.Register("  Network issue  ");
TicketReportService plain = new(store, new PlainTextFormatter());
TicketReportService compact = new(store, new CompactFormatter());
Console.WriteLine(plain.Report(first));
Console.WriteLine(compact.Report(first));
try { registration.Register("ab"); }
catch (ArgumentException) { Console.WriteLine("Invalid registration rejected."); }
Console.WriteLine($"Next successful ID: {registration.Register("Printer issue")}");
Console.WriteLine(plain.Report(99));
Console.WriteLine($"State after reporting: {store.FindById(first)?.Status}");
