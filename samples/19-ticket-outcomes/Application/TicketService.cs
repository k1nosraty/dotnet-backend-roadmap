using SupportDesk.Domain;
namespace SupportDesk.Application;

public sealed record TicketSummary(int Id, string Title);
public interface ITicketWriter { TicketSummary Add(TicketTitle title); }
public interface ITicketReader { TicketSummary? Find(int id); }
public sealed class TicketService
{
    private readonly ITicketWriter writer;
    private readonly ITicketReader reader;
    public TicketService(ITicketWriter writer, ITicketReader reader)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(reader);
        this.writer = writer; this.reader = reader;
    }
    public Outcome<TicketSummary> Register(string? rawTitle)
    {
        if (!TicketTitle.TryCreate(rawTitle, out TicketTitle? title))
            return Outcome<TicketSummary>.Failure(ErrorCode.InvalidInput, "Title must contain 3-120 characters.");
        return Outcome<TicketSummary>.Success(writer.Add(title));
    }
    public Outcome<TicketSummary> Find(int id)
    {
        if (id <= 0) return Outcome<TicketSummary>.Failure(ErrorCode.InvalidInput, "ID must be positive.");
        TicketSummary? found = reader.Find(id);
        return found is null ? Outcome<TicketSummary>.Failure(ErrorCode.NotFound, "Ticket not found.")
            : Outcome<TicketSummary>.Success(found);
    }
}
