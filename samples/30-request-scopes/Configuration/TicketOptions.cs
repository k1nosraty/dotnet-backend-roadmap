namespace SupportDesk.Configuration;
public sealed class TicketOptions
{
    public const string SectionName = "Tickets";
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 50;
}
