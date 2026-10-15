using SupportDesk.Domain;
namespace SupportDesk.Application;
public sealed class TicketService(ITicketStore store, RequestStamp stamp)
{
    public Guid RequestId => stamp.Id;
    public Ticket Create(string title) => store.Create(TicketTitle.Create(title));
    public Ticket? Find(int id) => store.Find(id);
    public Ticket[] All() => store.All();
    public MutationResult Rename(int id, string title) => store.Rename(id, TicketTitle.Create(title));
    public MutationResult AddNote(int id, string text) => store.Mutate(id, ticket => ticket.AddNote(text));
    public MutationResult ChangeStatus(int id, string status) => store.Mutate(id, ticket => ticket.ChangeStatus(status));
    public bool Delete(int id) => store.Delete(id);
}
