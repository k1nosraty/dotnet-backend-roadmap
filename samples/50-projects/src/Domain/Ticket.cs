using System.Collections.ObjectModel;
namespace SupportDesk.Domain;

public sealed record Ticket
{
    public int Id { get; }
    public string Title { get; }
    public string Status { get; }
    public ReadOnlyCollection<string> Notes { get; }

    private Ticket(int id, string title, string status, IEnumerable<string> notes)
    {
        Id = id;
        Title = title;
        Status = status;
        Notes = Array.AsReadOnly(notes.ToArray());
    }

    public static Ticket Create(int id, TicketTitle title)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        ArgumentNullException.ThrowIfNull(title);
        return new(id, title.Value, "Open", []);
    }

    public Ticket? Rename(TicketTitle title)
    {
        ArgumentNullException.ThrowIfNull(title);
        if (Status == "Closed") return null;
        return new(Id, title.Value, Status, Notes);
    }

    public Ticket? AddNote(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string normalized = text.Trim();
        if (normalized.Length < 3 || normalized.Length > 500)
            throw new ArgumentException("Note must contain 3-500 characters.", nameof(text));
        if (Status == "Closed" || Notes.Count >= 5 || Notes.Contains(normalized, StringComparer.Ordinal))
            return null;
        return new(Id, Title, Status, Notes.Append(normalized));
    }

    public Ticket? ChangeStatus(string next)
    {
        ArgumentNullException.ThrowIfNull(next);
        if (next == Status) return this;
        if (Status == "Open" && next == "InProgress") return new(Id, Title, next, Notes);
        if (Status == "InProgress" && next == "Closed" && Notes.Count > 0)
            return new(Id, Title, next, Notes);
        return null;
    }
}
