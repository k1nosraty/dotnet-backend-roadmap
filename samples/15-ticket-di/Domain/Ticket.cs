namespace SupportDesk.Domain;

public enum TicketStatus { Open, InProgress, Closed }

public sealed class Ticket
{
    private readonly List<string> notes = new();
    private readonly IReadOnlyList<string> notesView;

    public int Id { get; }
    public string Title { get; private set; }
    public TicketStatus Status { get; private set; } = TicketStatus.Open;
    public IReadOnlyList<string> Notes => notesView;

    public Ticket(int id, string title)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        Id = id;
        Title = NormalizeText(title, 3, 120, nameof(title));
        notesView = notes.AsReadOnly();
    }

    public void Rename(string title)
    {
        EnsureEditable();
        string normalized = NormalizeText(title, 3, 120, nameof(title));
        Title = normalized;
    }

    public void AddNote(string text)
    {
        EnsureEditable();
        string normalized = NormalizeText(text, 3, 500, nameof(text));
        if (notes.Count >= 5)
            throw new InvalidOperationException("A ticket can contain at most five notes.");
        if (notes.Contains(normalized, StringComparer.Ordinal))
            throw new InvalidOperationException("Duplicate notes are not allowed.");
        notes.Add(normalized);
    }

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
        if (notes.Count == 0)
            throw new InvalidOperationException("A resolution note is required before closing.");
        Status = TicketStatus.Closed;
    }

    private void EnsureEditable()
    {
        if (Status == TicketStatus.Closed)
            throw new InvalidOperationException("Closed tickets cannot be edited.");
    }

    private static string NormalizeText(string text, int minimum, int maximum, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(text, parameterName);
        string normalized = text.Trim();
        if (normalized.Length < minimum || normalized.Length > maximum)
            throw new ArgumentException($"Text length must be {minimum}-{maximum}.", parameterName);
        return normalized;
    }
}
