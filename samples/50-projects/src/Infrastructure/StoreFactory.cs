using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

// Only composition code needs this factory. Use cases depend on ITicketStore.
public static class StoreFactory
{
    public static ITicketStore Create(IEnumerable<Ticket> seed) => new InMemoryTicketStore(seed);
}
