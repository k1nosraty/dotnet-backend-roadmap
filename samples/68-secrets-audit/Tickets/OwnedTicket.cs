using System.Text.Json.Serialization;
namespace SupportDesk.Tickets;
// Deliberately small authorization lab model; not the full TeamDesk lifecycle aggregate.
public sealed class OwnedTicket
{
    public Guid Id { get; set; }
    public string OwnerId { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string Title { get; set; } = "";
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record TicketInput(string? Title);
public sealed record TicketView(Guid Id, string Title);
