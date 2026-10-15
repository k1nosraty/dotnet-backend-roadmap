using SupportDesk.Contracts;
using SupportDesk.Domain;
namespace SupportDesk.Mapping;
public static class TicketMapping
{
    public static TicketResponse ToResponse(this Ticket ticket) =>
        new(ticket.Id, ticket.Title, ticket.Status, ticket.Notes.ToArray());
}
