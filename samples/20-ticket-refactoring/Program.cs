using SupportDesk.Application;
using SupportDesk.Infrastructure;
using SupportDesk.Presentation;

InMemoryTicketStore store = new();
TicketService service = new(store, store);
_ = service.Register("Network issue");
return new ConsoleCommands(service).Execute(args);
