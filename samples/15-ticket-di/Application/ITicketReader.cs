using SupportDesk.Domain;
namespace SupportDesk.Application;

public interface ITicketReader
{
    // Positive ID required; null means absent. Infrastructure failures must not become null.
    Ticket? FindById(int id);
}
