namespace SupportDesk.Application;

public enum OperationCode { Success, Invalid, Missing, Conflict }
public sealed record TicketResponse(int Id, string Title, string Status, int NoteCount);
public sealed record OperationResult
{
    public OperationCode Code { get; }
    public TicketResponse? Ticket { get; }
    private OperationResult(OperationCode code, TicketResponse? ticket)
        => (Code, Ticket) = (code, ticket);

    public static OperationResult Success(TicketResponse ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return new(OperationCode.Success, ticket);
    }
    public static OperationResult Invalid() => new(OperationCode.Invalid, null);
    public static OperationResult Missing() => new(OperationCode.Missing, null);
    public static OperationResult Conflict() => new(OperationCode.Conflict, null);
}
