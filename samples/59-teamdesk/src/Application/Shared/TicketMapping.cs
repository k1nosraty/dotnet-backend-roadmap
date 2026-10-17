using SupportDesk.Domain;
namespace SupportDesk.Application;

public static class TicketMapping
{
    // One explicit projection. Version and note bodies are not in this public shape.
    public static TicketResponse ToResponse(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return new(ticket.Id, ticket.Title, ticket.Status, ticket.Notes.Count);
    }
}
