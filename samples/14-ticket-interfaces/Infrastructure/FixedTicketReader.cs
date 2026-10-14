using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

// A deterministic example implementation, not a persistent store.
public sealed class FixedTicketReader : ITicketReader
{
    public Ticket? FindById(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        return id == 10 ? new Ticket(10, "Known demo ticket") : null;
    }
}

public sealed class FailingTicketReader : ITicketReader
{
    public Ticket? FindById(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        throw new IOException("Demo reader is unavailable.");
    }
}
