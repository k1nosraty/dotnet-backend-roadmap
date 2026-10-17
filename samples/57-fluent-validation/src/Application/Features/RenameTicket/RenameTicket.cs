using SupportDesk.Domain;
namespace SupportDesk.Application;

public sealed record RenameTicketCommand(int Id, string? Title);
public sealed class RenameTicket(ITicketStore store)
{
    public async Task<OperationResult> ExecuteAsync(RenameTicketCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ct.ThrowIfCancellationRequested();
        string? text = command.Title?.Trim();
        if (command.Id <= 0 || text is null || text.Length < 3 || text.Length > 120)
            return OperationResult.Invalid();
        var original = await store.LoadAsync(command.Id, ct);
        if (original is null) return OperationResult.Missing();
        var changed = original.Value.Rename(TicketTitle.Create(text));
        if (changed is null) return OperationResult.Conflict();
        return (await store.SaveAsync(original, changed, ct)) switch
        {
            SaveOutcome.Saved => OperationResult.Success(TicketMapping.ToResponse(changed)),
            SaveOutcome.Missing => OperationResult.Missing(),
            SaveOutcome.Conflict => OperationResult.Conflict(),
            _ => throw new InvalidOperationException("Unknown store outcome.")
        };
    }
}
