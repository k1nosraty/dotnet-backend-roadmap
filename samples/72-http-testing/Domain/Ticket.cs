namespace SupportDesk.Domain;
public enum TicketStatus { Open, InProgress, Closed }
public sealed class Ticket
{
    public Guid Id { get; }
    public TicketTitle Title { get; }
    public TicketStatus Status { get; }
    public string? Resolution { get; }
    private Ticket(Guid id, TicketTitle title, TicketStatus status, string? resolution)
    { Id = id; Title = title; Status = status; Resolution = resolution; }
    public static Ticket Open(Guid id, TicketTitle title)
    {
        if (id == Guid.Empty) throw new ArgumentException("Identity is required.", nameof(id));
        ArgumentNullException.ThrowIfNull(title);
        return new(id, title, TicketStatus.Open, null);
    }
    public Ticket Start() => Status == TicketStatus.Open ? new(Id, Title, TicketStatus.InProgress, null)
        : throw new InvalidOperationException("Only an open ticket can start.");
    public Ticket Resolve(string? resolution)
    {
        if (Status != TicketStatus.InProgress) throw new InvalidOperationException("Only an in-progress ticket can resolve.");
        string trimmed = resolution?.Trim() ?? "";
        if (!TextRules.HasLength(trimmed, 200)) throw new ArgumentException("Resolution must contain 3 to 200 Unicode scalar values.", nameof(resolution));
        return new(Id, Title, TicketStatus.Closed, trimmed);
    }
}
