using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;
using SupportDesk.Features;

if (args.Contains("--check"))
{
    await Verification.RunAsync();
    AggregateChecks.Run();
    await AdapterChecks.RunAsync();
    await MappingChecks.RunAsync();
    await QueryChecks.RunAsync();
    await ValidationChecks.RunAsync();
    ArchitectureChecks.Run();
    return;
}
var builder = WebApplication.CreateBuilder(args);
// Fixed in-memory lab seed; one aggregate to exercise HTTP requests, not a product login.
var ticket = Ticket.Create(1, TicketTitle.Create("Network issue")).ChangeStatus("InProgress")!;
var ports = StoreFactory.CreateServices([ticket]);
builder.Services.AddSingleton<ITicketStore>(ports.Commands);
builder.Services.AddSingleton<ITicketQueries>(ports.Queries);
builder.Services.AddScoped<ListTickets>();
builder.Services.AddScoped<GetTicket>();
builder.Services.AddScoped<ResolveTicketValidator>();
builder.Services.AddScoped<ResolveTicket>();
builder.Services.AddScoped<RenameTicket>();
builder.Services.AddScoped<CloseTicket>();
var app = builder.Build();
app.MapTicketQueries();
app.MapResolveTicket();
app.MapRenameTicket();
app.MapCloseTicket();
app.Run();
