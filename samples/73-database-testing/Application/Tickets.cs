using SupportDesk.Domain;
namespace SupportDesk.Application;
public sealed record TicketSnapshot(Ticket Value, long Version, string OwnerId);
public enum SaveOutcome { Saved, Missing, Conflict }
public enum Outcome { Ok, Invalid, Missing, Conflict }
public sealed record TicketView(Guid Id, string Title, string Status, string? Resolution);
public sealed record Result(Outcome Outcome, TicketView? Ticket = null);
public interface ITicketStore
{
    Task<TicketSnapshot> CreateAsync(TicketTitle title, string owner, CancellationToken ct);
    Task<TicketSnapshot?> LoadAsync(Guid id, CancellationToken ct);
    Task<SaveOutcome> SaveAsync(TicketSnapshot expected, Ticket changed, CancellationToken ct);
}
public sealed class Tickets(ITicketStore store)
{
    public async Task<Result> CreateAsync(string owner, string? title, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string text = title?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(owner) || !TextRules.HasLength(text, 120)) return new(Outcome.Invalid);
        return new(Outcome.Ok, View((await store.CreateAsync(TicketTitle.Create(text), owner, ct)).Value));
    }
    public async Task<Result> GetAsync(string owner, Guid id, CancellationToken ct)
    {
        var row = await store.LoadAsync(id, ct);
        return row is null || row.OwnerId != owner ? new(Outcome.Missing) : new(Outcome.Ok, View(row.Value));
    }
    public async Task<Result> ChangeAsync(string owner, Guid id, bool resolve, string? text, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (resolve && !TextRules.HasLength(text?.Trim() ?? "", 200)) return new(Outcome.Invalid);
        var row = await store.LoadAsync(id, ct);
        if (row is null || row.OwnerId != owner) return new(Outcome.Missing);
        if (row.Value.Status != (resolve ? TicketStatus.InProgress : TicketStatus.Open)) return new(Outcome.Conflict);
        var changed = resolve ? row.Value.Resolve(text) : row.Value.Start();
        return await store.SaveAsync(row, changed, ct) switch
        {
            SaveOutcome.Saved => new(Outcome.Ok, View(changed)),
            SaveOutcome.Missing => new(Outcome.Missing),
            _ => new(Outcome.Conflict)
        };
    }
    private static TicketView View(Ticket ticket) => new(ticket.Id, ticket.Title.Value, ticket.Status.ToString(), ticket.Resolution);
}
