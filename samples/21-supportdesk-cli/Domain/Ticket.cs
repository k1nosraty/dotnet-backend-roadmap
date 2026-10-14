namespace SupportDesk.Domain;

public enum TicketStatus { Open, InProgress, Closed }
public enum TicketPriority { Low, Normal, High }
public sealed class Ticket
{
    private readonly List<string> notes = new();
    public int Id { get; }
    public TicketTitle Title { get; private set; }
    public TicketPriority Priority { get; }
    public TicketStatus Status { get; private set; }
    public IReadOnlyList<string> Notes { get; }
    public Ticket(int id, TicketTitle title, TicketPriority priority)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        ArgumentNullException.ThrowIfNull(title);
        if (!Enum.IsDefined(priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        Id = id; Title = title; Priority = priority; Notes = notes.AsReadOnly();
    }
    public void Rename(TicketTitle title)
    {
        EnsureEditable(); ArgumentNullException.ThrowIfNull(title); Title = title;
    }
    public void AddNote(string text)
    {
        EnsureEditable(); ArgumentNullException.ThrowIfNull(text);
        string normalized = text.Trim();
        if (normalized.Length < 3 || normalized.Length > 500) throw new ArgumentException("Note must contain 3-500 characters.", nameof(text));
        if (notes.Count >= 5 || notes.Contains(normalized, StringComparer.Ordinal)) throw new InvalidOperationException("Note capacity or duplicate rule failed.");
        notes.Add(normalized);
    }
    public void Start()
    {
        if (Status != TicketStatus.Open) throw new InvalidOperationException("Only open tickets can start.");
        Status = TicketStatus.InProgress;
    }
    public void Close()
    {
        if (Status != TicketStatus.InProgress || notes.Count == 0) throw new InvalidOperationException("Closing requires an in-progress ticket and a resolution note.");
        Status = TicketStatus.Closed;
    }
    private void EnsureEditable()
    {
        if (Status == TicketStatus.Closed) throw new InvalidOperationException("Closed tickets cannot be edited.");
    }
    public static Ticket Restore(int id, TicketTitle title, TicketPriority priority, TicketStatus status, IEnumerable<string> notes)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        ArgumentNullException.ThrowIfNull(notes);
        Ticket ticket = new(id, title, priority);
        foreach (string note in notes) ticket.AddNote(note);
        if (status is TicketStatus.InProgress or TicketStatus.Closed) ticket.Start();
        if (status == TicketStatus.Closed) ticket.Close();
        return ticket;
    }
}
