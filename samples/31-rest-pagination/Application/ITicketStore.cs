using SupportDesk.Domain;
namespace SupportDesk.Application;
public interface ITicketStore
{
    Ticket Create(TicketTitle title);
    Ticket? Find(int id);
    Ticket[] All();
    Ticket? Rename(int id, TicketTitle title);
    bool Delete(int id);
}
