using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Presentation;

public sealed class PlainTextFormatter : ITicketFormatter
{
    public string Format(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return $"#{ticket.Id}: {ticket.Title} ({ticket.Status})";
    }
}
public sealed class CompactFormatter : ITicketFormatter
{
    public string Format(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return $"#{ticket.Id} {ticket.Status}";
    }
}
