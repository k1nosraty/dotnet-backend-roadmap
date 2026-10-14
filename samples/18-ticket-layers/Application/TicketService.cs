using SupportDesk.Domain;
namespace SupportDesk.Application;

public sealed class TicketService
{
    private readonly ITicketCreator creator;
    private readonly ITicketReader reader;
    public TicketService(ITicketCreator creator, ITicketReader reader)
    {
        ArgumentNullException.ThrowIfNull(creator);
        ArgumentNullException.ThrowIfNull(reader);
        this.creator = creator;
        this.reader = reader;
    }
    public TicketSummary Register(string rawTitle)
    {
        TicketTitle title = TicketTitle.Create(rawTitle);
        Ticket ticket = creator.Create(title);
        return new TicketSummary(ticket.Id, ticket.Title.Value);
    }
    public TicketSummary? Find(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        Ticket? ticket = reader.FindById(id);
        return ticket is null ? null : new TicketSummary(ticket.Id, ticket.Title.Value);
    }
}
