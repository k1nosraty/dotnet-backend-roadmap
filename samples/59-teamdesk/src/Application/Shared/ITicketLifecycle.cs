using SupportDesk.Domain;
namespace SupportDesk.Application;

public interface ITicketLifecycle
{
    Task<Ticket?> CreateAsync(TicketTitle title, CancellationToken ct);
    Task<SaveOutcome> DeleteAsync(TicketSnapshot expected, CancellationToken ct);
}
