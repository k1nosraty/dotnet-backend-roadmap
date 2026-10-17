using SupportDesk.Domain;
namespace SupportDesk.Application;

public sealed record ResolveTicketCommand(int Id, string? Resolution);

// This service orchestrates a use case; it does not duplicate state-transition rules.
public sealed class ResolveTicket(ITicketStore store)
{
    public async Task<OperationResult> ExecuteAsync(ResolveTicketCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ct.ThrowIfCancellationRequested();
        string? text = command.Resolution?.Trim();
        if (command.Id <= 0 || text is null || text.Length < 3 || text.Length > 500)
            return OperationResult.Invalid();

        var original = await store.LoadAsync(command.Id, ct);
        if (original is null) return OperationResult.Missing();
        var withNote = original.Value.AddNote(text);
        if (withNote is null) return OperationResult.Conflict();
        var closed = withNote.ChangeStatus("Closed");
        if (closed is null) return OperationResult.Conflict();

        // No persistence occurred after AddNote. Both changes commit together.
        var saved = await store.SaveAsync(original, closed, ct);
        return saved switch
        {
            SaveOutcome.Missing => OperationResult.Missing(),
            SaveOutcome.Conflict => OperationResult.Conflict(),
            SaveOutcome.Saved => OperationResult.Success(TicketMapping.ToResponse(closed)),
            _ => throw new InvalidOperationException("Unknown store outcome.")
        };
    }
}
