namespace SupportDesk.Application;

public sealed class ListTickets(ITicketQueries queries)
{
    public Task<TicketPage?> ExecuteAsync(TicketListQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        ct.ThrowIfCancellationRequested();
        return query.IsValid ? RunAsync(query, ct) : Task.FromResult<TicketPage?>(null);
    }
    private async Task<TicketPage?> RunAsync(TicketListQuery query, CancellationToken ct)
        => await queries.ListAsync(query, ct);
}
public sealed class GetTicket(ITicketQueries queries)
{
    public Task<TicketResponse?> ExecuteAsync(int id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return id > 0 ? queries.FindAsync(id, ct) : Task.FromResult<TicketResponse?>(null);
    }
}
