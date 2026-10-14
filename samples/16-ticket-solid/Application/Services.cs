namespace SupportDesk.Application;

public sealed class TicketRegistrationService
{
    private readonly ITicketWriter writer;
    public TicketRegistrationService(ITicketWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        this.writer = writer;
    }
    public int Register(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        string normalized = title.Trim();
        if (normalized.Length < 3 || normalized.Length > 120)
            throw new ArgumentException("Title must contain 3-120 characters.", nameof(title));
        return writer.Add(normalized);
    }
}

public sealed class TicketReportService
{
    private readonly ITicketReader reader;
    private readonly ITicketFormatter formatter;
    public TicketReportService(ITicketReader reader, ITicketFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(formatter);
        this.reader = reader;
        this.formatter = formatter;
    }
    public string Report(int id)
    {
        var ticket = reader.FindById(id);
        return ticket is null ? "Ticket not found." : formatter.Format(ticket);
    }
}
