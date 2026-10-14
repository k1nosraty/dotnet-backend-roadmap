using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

public sealed class InMemoryTicketStore : ITicketWriter, ITicketReader
{
    private readonly Dictionary<int, TicketSummary> tickets = new();
    private long nextId = 1;
    public TicketSummary Add(TicketTitle title)
    {
        ArgumentNullException.ThrowIfNull(title);
        if (nextId > int.MaxValue) throw new InvalidOperationException("ID capacity reached.");
        TicketSummary ticket = new((int)nextId, title.Value);
        tickets.Add(ticket.Id, ticket); nextId++;
        return ticket;
    }
    public TicketSummary? Find(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        return tickets.TryGetValue(id, out TicketSummary? found) ? found : null;
    }
}
public sealed class FailingWriter : ITicketWriter
{
    public TicketSummary Add(TicketTitle title) => throw new IOException("Demo persistence failure.");
}
