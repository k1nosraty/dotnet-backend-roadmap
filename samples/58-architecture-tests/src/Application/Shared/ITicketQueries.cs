namespace SupportDesk.Application;

public sealed record TicketListQuery(string? Q = null, string? Status = null, int Page = 1, int PageSize = 20)
{
    public bool IsValid => Page > 0 && PageSize is >= 1 and <= 50 &&
        (Q is null || Q.Trim().Length <= 120) &&
        (Status is null or "Open" or "InProgress" or "Closed");
}
public sealed record TicketPage(TicketResponse[] Items, int Total, int Page, int PageSize);
public interface ITicketQueries
{
    Task<TicketResponse?> FindAsync(int id, CancellationToken ct);
    Task<TicketPage> ListAsync(TicketListQuery query, CancellationToken ct);
}
