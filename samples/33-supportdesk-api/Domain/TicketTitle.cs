namespace SupportDesk.Domain;

public sealed record TicketTitle
{
    public string Value { get; }
    private TicketTitle(string value) => Value = value;

    public static TicketTitle Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string normalized = value.Trim();
        if (normalized.Length < 3 || normalized.Length > 120)
            throw new ArgumentException("Title must contain 3-120 characters.", nameof(value));
        return new TicketTitle(normalized);
    }
    public override string ToString() => Value;
}
