using SupportDesk.Domain;
namespace SupportDesk.Application;

public interface ITicketCreator
{
    // Return only after persistence succeeds; invalid input or I/O failure must not be a success.
    Ticket Create(TicketTitle title);
}
public interface ITicketReader
{
    Ticket? FindById(int id);
}
public sealed record TicketSummary(int Id, string Title);
