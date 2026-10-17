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
    return;
}
var builder = WebApplication.CreateBuilder(args);
// Fixed in-memory lab seed; one aggregate to exercise HTTP requests, not a product login.
var ticket = Ticket.Create(1, TicketTitle.Create("Network issue")).ChangeStatus("InProgress")!;
builder.Services.AddSingleton<ITicketStore>(StoreFactory.Create([ticket]));
builder.Services.AddScoped<ResolveTicket>();
builder.Services.AddScoped<RenameTicket>();
builder.Services.AddScoped<CloseTicket>();
var app = builder.Build();
app.MapResolveTicket();
app.MapRenameTicket();
app.MapCloseTicket();
app.Run();
