using SupportDesk.Domain;
namespace SupportDesk.Application;
public interface ITicketStore
{
    Ticket Create(TicketTitle title);
    Ticket? Find(int id);
    Ticket[] All();
    Ticket? Rename(int id, TicketTitle title);
    MutationResult Mutate(int id, Func<Ticket, Ticket?> change);
    bool Delete(int id);
}
