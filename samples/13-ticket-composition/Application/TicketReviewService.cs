using SupportDesk.Domain;
using SupportDesk.Routing;
namespace SupportDesk.Application;

public sealed class TicketReviewService
{
    private readonly TicketReviewPolicy policy;
    public TicketReviewService(TicketReviewPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        this.policy = policy;
    }
    public string GetDecision(Ticket ticket) => policy.NeedsReview(ticket) ? "Review required" : "No review";
}
