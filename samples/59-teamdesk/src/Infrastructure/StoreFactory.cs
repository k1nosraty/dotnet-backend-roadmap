using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

// Only composition code needs this factory. Use cases depend on ITicketStore.
public sealed record StoreServices(ITicketStore Commands, ITicketQueries Queries, ITicketLifecycle Lifecycle);

public static class StoreFactory
{
    public static StoreServices CreateServices(IEnumerable<Ticket> seed)
    {
        var store = new InMemoryTicketStore(seed);
        return new(store, store, store);
    }
    public static StoreServices CreateFileServices(string path, IEnumerable<Ticket> seed)
    {
        var store = new JsonTicketStore(path, seed);
        return new(store, store, store);
    }
    public static ITicketStore CreateFile(string path, IEnumerable<Ticket> seed) => new JsonTicketStore(path, seed);
    public static ITicketStore Create(IEnumerable<Ticket> seed) => new InMemoryTicketStore(seed);
}
