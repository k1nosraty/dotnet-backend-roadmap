using SupportDesk.Domain;
namespace SupportDesk.Application;

public sealed class CloseTicket(ITicketStore store)
{
    public async Task<OperationResult> ExecuteAsync(int id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (id <= 0) return OperationResult.Invalid();
        var original = await store.LoadAsync(id, ct);
        if (original is null) return OperationResult.Missing();

        // The domain owns the rule: InProgress + at least one note before Closed.
        var changed = original.Value.ChangeStatus("Closed");
        if (changed is null) return OperationResult.Conflict();
        var saved = await store.SaveAsync(original, changed, ct);
        return saved switch
        {
            SaveOutcome.Missing => OperationResult.Missing(),
            SaveOutcome.Conflict => OperationResult.Conflict(),
            SaveOutcome.Saved => OperationResult.Success(TicketMapping.ToResponse(changed)),
            _ => throw new InvalidOperationException("Unknown store outcome.")
        };
    }
}
