using SupportDesk.Domain;
namespace SupportDesk.Application;

public sealed class TicketService
{
    private readonly ITicketStore store;
    public TicketService(ITicketStore store) { ArgumentNullException.ThrowIfNull(store); this.store = store; }
    public async Task<Outcome<TicketSummary>> AddAsync(string raw, TicketPriority priority, CancellationToken token)
    {
        if (!TicketTitle.TryCreate(raw, out TicketTitle? title) || !Enum.IsDefined(priority))
            return Outcome<TicketSummary>.Failure(ErrorCode.InvalidInput, "Invalid title or priority.");
        TicketState state = await store.LoadAsync(token);
        if (state.NextId > int.MaxValue) return Outcome<TicketSummary>.Failure(ErrorCode.CapacityReached, "ID capacity reached.");
        Ticket ticket = new((int)state.NextId, title, priority);
        state.Tickets.Add(ticket); state.NextId++;
        await store.SaveAsync(state, token);
        return Outcome<TicketSummary>.Success(TicketMapping.ToSummary(ticket));
    }
    public async Task<Outcome<TicketSummary>> FindAsync(int id, CancellationToken token)
    {
        if (id <= 0) return InvalidId();
        TicketState state = await store.LoadAsync(token);
        Ticket? ticket = state.Tickets.FirstOrDefault(t => t.Id == id);
        return ticket is null ? Missing() : Outcome<TicketSummary>.Success(TicketMapping.ToSummary(ticket));
    }
    public async Task<TicketList> ListAsync(bool openOnly, CancellationToken token)
    {
        TicketState state = await store.LoadAsync(token);
        var selected = state.Tickets.Where(t => !openOnly || t.Status == TicketStatus.Open).OrderBy(t => t.Id);
        return new TicketList(selected.Select(TicketMapping.ToSummary).ToArray());
    }
    public async Task<TicketReport> ReportAsync(CancellationToken token)
    {
        TicketState state = await store.LoadAsync(token);
        return new(state.Tickets.Count(t => t.Status == TicketStatus.Open), state.Tickets.Count(t => t.Status == TicketStatus.InProgress), state.Tickets.Count(t => t.Status == TicketStatus.Closed));
    }
    public Task<Outcome<TicketSummary>> RenameAsync(int id, string raw, CancellationToken token) => ChangeAsync(id, ticket =>
    {
        if (!TicketTitle.TryCreate(raw, out TicketTitle? title)) return Outcome<TicketSummary>.Failure(ErrorCode.InvalidInput, "Invalid title.");
        if (ticket.Status == TicketStatus.Closed) return InvalidState();
        ticket.Rename(title); return Success(ticket);
    }, token);
    public Task<Outcome<TicketSummary>> NoteAsync(int id, string raw, CancellationToken token) => ChangeAsync(id, ticket =>
    {
        if (raw is null) return Outcome<TicketSummary>.Failure(ErrorCode.InvalidInput, "Invalid note.");
        string text = raw.Trim();
        if (text.Length < 3 || text.Length > 500) return Outcome<TicketSummary>.Failure(ErrorCode.InvalidInput, "Invalid note.");
        if (ticket.Status == TicketStatus.Closed || ticket.Notes.Count >= 5 || ticket.Notes.Contains(text, StringComparer.Ordinal)) return InvalidState();
        ticket.AddNote(text); return Success(ticket);
    }, token);
    public Task<Outcome<TicketSummary>> StartAsync(int id, CancellationToken token) => ChangeAsync(id, ticket =>
    {
        if (ticket.Status != TicketStatus.Open) return InvalidState();
        ticket.Start(); return Success(ticket);
    }, token);
    public Task<Outcome<TicketSummary>> CloseAsync(int id, CancellationToken token) => ChangeAsync(id, ticket =>
    {
        if (ticket.Status != TicketStatus.InProgress || ticket.Notes.Count == 0) return InvalidState();
        ticket.Close(); return Success(ticket);
    }, token);
    public async Task<Outcome<TicketSummary>> DeleteAsync(int id, CancellationToken token)
    {
        if (id <= 0) return InvalidId();
        TicketState state = await store.LoadAsync(token);
        Ticket? ticket = state.Tickets.FirstOrDefault(t => t.Id == id);
        if (ticket is null) return Missing();
        state.Tickets.Remove(ticket);
        await store.SaveAsync(state, token);
        return Success(ticket);
    }
    private async Task<Outcome<TicketSummary>> ChangeAsync(int id, Func<Ticket, Outcome<TicketSummary>> change, CancellationToken token)
    {
        if (id <= 0) return InvalidId();
        TicketState state = await store.LoadAsync(token);
        Ticket? ticket = state.Tickets.FirstOrDefault(t => t.Id == id);
        if (ticket is null) return Missing();
        Outcome<TicketSummary> result = change(ticket);
        if (result.IsSuccess) await store.SaveAsync(state, token);
        return result;
    }
    private static Outcome<TicketSummary> Success(Ticket ticket) => Outcome<TicketSummary>.Success(TicketMapping.ToSummary(ticket));
    private static Outcome<TicketSummary> InvalidId() => Outcome<TicketSummary>.Failure(ErrorCode.InvalidInput, "ID must be positive.");
    private static Outcome<TicketSummary> Missing() => Outcome<TicketSummary>.Failure(ErrorCode.NotFound, "Ticket not found.");
    private static Outcome<TicketSummary> InvalidState() => Outcome<TicketSummary>.Failure(ErrorCode.InvalidState, "This change is not allowed by the ticket rules.");
}
