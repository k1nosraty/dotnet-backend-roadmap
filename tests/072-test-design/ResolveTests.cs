using SupportDesk.Application;
using SupportDesk.Domain;
using Xunit;
namespace SupportDesk.Tests;
public sealed class ResolveTests
{
    private static TicketSnapshot Started() => new(Ticket.Open(Guid.NewGuid(), TicketTitle.Create("Network")).Start(), 7);
    [Theory] [InlineData(null)] [InlineData("")] [InlineData("ab")]
    public async Task Invalid_input_does_not_touch_store(string? value)
    {
        var store = new RecordingStore(Started());
        Assert.Equal(ResolveOutcome.Invalid, await new ResolveTicket(store).ExecuteAsync(store.Row!.Value.Id, value, default));
        Assert.Equal(0, store.Loads);
        Assert.Null(store.Changed);
    }
    [Fact]
    public async Task Missing_is_distinct_from_conflict()
    {
        var store = new RecordingStore(null);
        Assert.Equal(ResolveOutcome.Missing, await new ResolveTicket(store).ExecuteAsync(Guid.NewGuid(), "Cable fixed", default));
        Assert.Null(store.Changed);
    }
    [Theory] [InlineData(SaveOutcome.Saved, ResolveOutcome.Resolved)]
    [InlineData(SaveOutcome.Conflict, ResolveOutcome.Conflict)] [InlineData(SaveOutcome.Missing, ResolveOutcome.Missing)]
    public async Task Maps_commit_outcome_without_mutating_loaded_snapshot(SaveOutcome saved, ResolveOutcome expected)
    {
        var row = Started(); var store = new RecordingStore(row) { Outcome = saved };
        using var source = new CancellationTokenSource();
        var result = await new ResolveTicket(store).ExecuteAsync(row.Value.Id, " Cable fixed ", source.Token);
        Assert.Equal(expected, result);
        Assert.Equal(source.Token, store.SeenToken);
        Assert.Equal(7, store.ExpectedVersion);
        Assert.Equal(TicketStatus.Closed, store.Changed!.Status);
        Assert.Equal("Cable fixed", store.Changed.Resolution);
        Assert.Equal(TicketStatus.InProgress, row.Value.Status);
    }
    [Fact]
    public async Task Wrong_state_never_attempts_commit()
    {
        var row = Started(); var store = new RecordingStore(new(row.Value.Resolve("Already fixed"), 8));
        Assert.Equal(ResolveOutcome.Conflict, await new ResolveTicket(store).ExecuteAsync(row.Value.Id, "Again", default));
        Assert.Null(store.Changed);
    }
    [Fact]
    public async Task Pre_cancelled_request_does_not_load()
    {
        var store = new RecordingStore(Started()); using var source = new CancellationTokenSource(); source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ResolveTicket(store).ExecuteAsync(store.Row!.Value.Id, "Cable fixed", source.Token));
        Assert.Equal(0, store.Loads);
    }
    [Fact]
    public async Task Failed_commit_does_not_become_success()
    {
        var store = new RecordingStore(Started()) { FailSave = true };
        await Assert.ThrowsAsync<IOException>(() => new ResolveTicket(store).ExecuteAsync(store.Row!.Value.Id, "Cable fixed", default));
        Assert.Equal(TicketStatus.InProgress, store.Row!.Value.Status);
    }
    private sealed class RecordingStore(TicketSnapshot? row) : ITicketStore
    {
        public TicketSnapshot? Row { get; } = row;
        public int Loads { get; private set; }
        public Ticket? Changed { get; private set; }
        public long ExpectedVersion { get; private set; }
        public CancellationToken SeenToken { get; private set; }
        public SaveOutcome Outcome { get; init; } = SaveOutcome.Saved;
        public bool FailSave { get; init; }
        public Task<TicketSnapshot?> LoadAsync(Guid id, CancellationToken ct)
        { Loads++; ct.ThrowIfCancellationRequested(); return Task.FromResult(Row); }
        public Task<SaveOutcome> SaveAsync(TicketSnapshot expected, Ticket changed, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); SeenToken = ct; ExpectedVersion = expected.Version;
            if (FailSave) throw new IOException("Deliberate test-store outage.");
            Changed = changed; return Task.FromResult(Outcome);
        }
    }
}
