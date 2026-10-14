using System.Diagnostics.CodeAnalysis;
namespace SupportDesk.Domain;

public sealed record TicketTitle
{
    public string Value { get; }
    private TicketTitle(string value) => Value = value;
    public static bool TryCreate(string? raw, [NotNullWhen(true)] out TicketTitle? title)
    {
        string? normalized = raw?.Trim();
        if (normalized is null || normalized.Length < 3 || normalized.Length > 120)
        { title = null; return false; }
        title = new TicketTitle(normalized);
        return true;
    }
    public static TicketTitle Create(string raw)
    {
        if (!TryCreate(raw, out TicketTitle? title))
            throw new ArgumentException("Title must contain 3-120 characters.", nameof(raw));
        return title;
    }
}
