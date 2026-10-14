namespace SupportDesk.Domain;

public enum TicketStatus { Open, InProgress, Closed }

public sealed class Ticket
{
    public int Id { get; }
    public string Title { get; private set; }
    public TicketStatus Status { get; private set; } = TicketStatus.Open;

    public Ticket(int id, string title)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        Id = id;
        Title = NormalizeTitle(title);
    }

    public void Rename(string title) => Title = NormalizeTitle(title);

    public void Start()
    {
        if (Status != TicketStatus.Open)
            throw new InvalidOperationException("Only open tickets can be started.");
        Status = TicketStatus.InProgress;
    }

    public void Close()
    {
        if (Status != TicketStatus.InProgress)
            throw new InvalidOperationException("Only in-progress tickets can be closed.");
        Status = TicketStatus.Closed;
    }

    private static string NormalizeTitle(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        string normalized = title.Trim();
        if (normalized.Length < 3 || normalized.Length > 120)
            throw new ArgumentException("Title must contain 3-120 characters.", nameof(title));
        return normalized;
    }
}
