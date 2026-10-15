using SupportDesk.Domain;
namespace SupportDesk.Application;
public sealed class TicketService(ITicketStore store)
{
    public Ticket Create(string title) => store.Create(TicketTitle.Create(title));
    public Ticket? Find(int id) => store.Find(id);
    public Ticket[] All() => store.All();
    public Ticket? Rename(int id, string title) => store.Rename(id, TicketTitle.Create(title));
    public bool Delete(int id) => store.Delete(id);
}
