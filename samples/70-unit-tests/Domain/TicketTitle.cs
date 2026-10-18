namespace SupportDesk.Domain;
public sealed record TicketTitle
{
    public string Value { get; }
    private TicketTitle(string value) => Value = value;
    public static TicketTitle Create(string? value)
    {
        string trimmed = value?.Trim() ?? "";
        if (!TextRules.HasLength(trimmed, 120)) throw new ArgumentException("Title must contain 3 to 120 Unicode scalar values.", nameof(value));
        return new(trimmed);
    }
}
