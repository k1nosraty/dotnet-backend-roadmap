using SupportDesk.Domain;
namespace SupportDesk.Application;

public sealed class TicketState
{
    public long NextId { get; set; } = 1;
    public List<Ticket> Tickets { get; } = new();
}
public interface ITicketStore
{
    Task<TicketState> LoadAsync(CancellationToken token);
    Task SaveAsync(TicketState state, CancellationToken token);
}
public sealed record TicketSummary(int Id, string Title, TicketPriority Priority, TicketStatus Status, IReadOnlyList<string> Notes);
public sealed record TicketList(IReadOnlyList<TicketSummary> Items);
public sealed record TicketReport(int Open, int InProgress, int Closed);
public static class TicketMapping
{
    public static TicketSummary ToSummary(Ticket ticket) => new(ticket.Id, ticket.Title.Value, ticket.Priority, ticket.Status, ticket.Notes.ToArray());
}
