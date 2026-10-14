using SupportDesk.Domain;
namespace SupportDesk.Reporting;

public abstract class TicketReport
{
    public string Generate(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return Format(ticket);
    }
    // Implementations format data; they must not change the ticket.
    protected abstract string Format(Ticket ticket);
}

public sealed class PlainTextTicketReport : TicketReport
{
    protected override string Format(Ticket ticket) => $"#{ticket.Id}: {ticket.Title} ({ticket.Status})";
}

public sealed class DetailedTicketReport : TicketReport
{
    protected override string Format(Ticket ticket) => $"#{ticket.Id}: {ticket.Title}; notes={ticket.Notes.Count}";
}
