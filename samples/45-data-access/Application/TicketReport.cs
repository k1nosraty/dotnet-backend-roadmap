namespace SupportDesk.Application;
public sealed record TicketSummary(int Id, string Title, int NoteCount);
public interface ITicketQueries { Task<TicketSummary[]> ListAsync(CancellationToken ct); }
public sealed class TicketReport(ITicketQueries queries)
{
 public Task<TicketSummary[]> RunAsync(CancellationToken ct) => queries.ListAsync(ct);
}
