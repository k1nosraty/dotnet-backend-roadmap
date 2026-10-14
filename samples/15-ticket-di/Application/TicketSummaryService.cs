namespace SupportDesk.Application;

public sealed class TicketSummaryService
{
    private readonly ITicketReader reader;
    public TicketSummaryService(ITicketReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        this.reader = reader;
    }
    public string GetSummary(int id)
    {
        var ticket = reader.FindById(id);
        return ticket is null ? "Ticket not found." : $"#{ticket.Id}: {ticket.Title} ({ticket.Status})";
    }
}
