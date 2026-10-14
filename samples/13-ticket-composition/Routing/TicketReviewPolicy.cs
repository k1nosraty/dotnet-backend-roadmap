using SupportDesk.Domain;
namespace SupportDesk.Routing;

public sealed class TicketReviewPolicy
{
    public int NoteThreshold { get; }
    public TicketReviewPolicy(int noteThreshold)
    {
        if (noteThreshold < 1 || noteThreshold > 5)
            throw new ArgumentOutOfRangeException(nameof(noteThreshold));
        NoteThreshold = noteThreshold;
    }
    public bool NeedsReview(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return ticket.Status != TicketStatus.Closed && ticket.Notes.Count >= NoteThreshold;
    }
}
