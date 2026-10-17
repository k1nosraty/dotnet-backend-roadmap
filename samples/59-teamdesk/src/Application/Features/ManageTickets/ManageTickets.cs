using SupportDesk.Domain;
namespace SupportDesk.Application;

public sealed record CreateTicketCommand(string? Title);
public sealed class CreateTicket(ITicketLifecycle lifecycle)
{
    public async Task<OperationResult> ExecuteAsync(CreateTicketCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ct.ThrowIfCancellationRequested();
        string? text = command.Title?.Trim();
        if (text is null || text.Length is < 3 or > 120) return OperationResult.Invalid();
        var created = await lifecycle.CreateAsync(TicketTitle.Create(text), ct);
        return created is null ? OperationResult.Conflict() : OperationResult.Success(TicketMapping.ToResponse(created));
    }
}
public sealed class DeleteTicket(ITicketStore store, ITicketLifecycle lifecycle)
{
    public async Task<OperationResult> ExecuteAsync(int id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (id <= 0) return OperationResult.Invalid();
        var original = await store.LoadAsync(id, ct);
        if (original is null) return OperationResult.Missing();
        return (await lifecycle.DeleteAsync(original, ct)) switch
        {
            SaveOutcome.Saved => OperationResult.Success(TicketMapping.ToResponse(original.Value)),
            SaveOutcome.Missing => OperationResult.Missing(),
            SaveOutcome.Conflict => OperationResult.Conflict(),
            _ => throw new InvalidOperationException("Unknown store outcome.")
        };
    }
}
public sealed record AddNoteCommand(int Id, string? Text);
public sealed record ChangeStatusCommand(int Id, string? Status);
public sealed class ChangeTicket(ITicketStore store)
{
    public Task<OperationResult> AddNoteAsync(AddNoteCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ct.ThrowIfCancellationRequested();
        string? text = command.Text?.Trim();
        if (text is null || text.Length is < 3 or > 500) return Task.FromResult(OperationResult.Invalid());
        return ApplyAsync(command.Id, t => t.AddNote(text), ct);
    }
    public Task<OperationResult> StatusAsync(ChangeStatusCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ct.ThrowIfCancellationRequested();
        if (command.Status is not ("Open" or "InProgress" or "Closed")) return Task.FromResult(OperationResult.Invalid());
        return ApplyAsync(command.Id, t => t.ChangeStatus(command.Status), ct);
    }
    private async Task<OperationResult> ApplyAsync(int id, Func<Ticket, Ticket?> change, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (id <= 0) return OperationResult.Invalid();
        var original = await store.LoadAsync(id, ct);
        if (original is null) return OperationResult.Missing();
        var changed = change(original.Value);
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
