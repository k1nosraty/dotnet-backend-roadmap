using System.Collections.ObjectModel;
namespace SupportDesk.Domain;
public sealed record Ticket
{
    public int Id { get; }
    public string Title { get; }
    public string Status { get; }
    public ReadOnlyCollection<string> Notes { get; }
    private Ticket(int id, string title, string status, IEnumerable<string> notes)
    { Id = id; Title = title; Status = status; Notes = Array.AsReadOnly(notes.ToArray()); }
    public static Ticket Create(int id, TicketTitle title) => new(id, title.Value, "Open", []);
    public Ticket Rename(TicketTitle title) => new(Id, title.Value, Status, Notes);
    public Ticket AddNote(string text)
    {
        string normalized = text.Trim();
        if (normalized.Length < 1 || normalized.Length > 500)
            throw new ArgumentException("Note must contain 1-500 characters.", nameof(text));
        return new(Id, Title, Status, Notes.Append(normalized));
    }
    public Ticket? ChangeStatus(string next)
    {
        if (next == Status) return this;
        if (Status == "Open" && next == "InProgress") return new(Id, Title, next, Notes);
        if (Status == "InProgress" && next == "Closed" && Notes.Count > 0)
            return new(Id, Title, next, Notes);
        return null;
    }
}
