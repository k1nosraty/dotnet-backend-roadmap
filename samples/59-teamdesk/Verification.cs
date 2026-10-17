using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

internal static class Verification
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static void Check(bool value, string scenario)
    {
        if (!value) throw new InvalidOperationException(scenario);
    }
    private static Ticket New(int id = 1) => Ticket.Create(id, TicketTitle.Create("Network issue"));
    private static Ticket Ready() => New().ChangeStatus("InProgress")!.AddNote("Resolved cable")!;

    public static async Task RunAsync()
    {
        var store = StoreFactory.Create([Ready()]);
        var service = new CloseTicket(store);
        Check((await service.ExecuteAsync(0, Ct)).Code == OperationCode.Invalid, "invalid id");
        Check((await service.ExecuteAsync(99, Ct)).Code == OperationCode.Missing, "missing id");
        var result = await service.ExecuteAsync(1, Ct);
        Check(result.Code == OperationCode.Success && result.Ticket?.Status == "Closed", "close");
        var closed = (await store.LoadAsync(1, Ct))!;
        Check(closed.Value.Status == "Closed" && closed.Version == 2, "persisted close");
        Check(closed.Value.Rename(TicketTitle.Create("New title")) is null, "closed rename rule");
        Check(closed.Value.AddNote("Another note") is null, "closed note rule");
        Check((await service.ExecuteAsync(1, Ct)).Code == OperationCode.Success, "idempotent close outcome");

        foreach (var seed in new[] { New(), New().ChangeStatus("InProgress")! })
        {
            var blocked = StoreFactory.Create([seed]);
            var before = await blocked.LoadAsync(1, Ct);
            Check((await new CloseTicket(blocked).ExecuteAsync(1, Ct)).Code == OperationCode.Conflict,
                "domain rule is enforced through application");
            Check(await blocked.LoadAsync(1, Ct) == before, "rejected change is not saved");
        }

        var racing = StoreFactory.Create([Ready()]);
        var snapshot = (await racing.LoadAsync(1, Ct))!;
        var a = snapshot.Value.Rename(TicketTitle.Create("First title"))!;
        var b = snapshot.Value.Rename(TicketTitle.Create("Second title"))!;
        var outcomes = await Task.WhenAll(
            Task.Run(() => racing.SaveAsync(snapshot, a, Ct)),
            Task.Run(() => racing.SaveAsync(snapshot, b, Ct)));
        Check(outcomes.Count(x => x == SaveOutcome.Saved) == 1 &&
              outcomes.Count(x => x == SaveOutcome.Conflict) == 1, "one stale writer loses");
        Check((await racing.LoadAsync(1, Ct))?.Version == 2, "exactly one commit");

        var beforeCancel = await racing.LoadAsync(1, Ct);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        try
        {
            await racing.SaveAsync(beforeCancel!, a, cancelled.Token);
            throw new InvalidOperationException("Expected cancellation.");
        }
        catch (OperationCanceledException) { }
        Check(await racing.LoadAsync(1, Ct) == beforeCancel, "cancelled save preserves state");
        try
        {
            await new CloseTicket(racing).ExecuteAsync(1, cancelled.Token);
            throw new InvalidOperationException("Expected service cancellation.");
        }
        catch (OperationCanceledException) { }
        try
        {
            await racing.SaveAsync(beforeCancel!, New(2), Ct);
            throw new InvalidOperationException("Expected identity guard.");
        }
        catch (ArgumentException) { }
        Check(await racing.LoadAsync(1, Ct) == beforeCancel, "identity failure preserves state");
        Check(await racing.SaveAsync(new TicketSnapshot(New(99), 1), New(99), Ct) == SaveOutcome.Missing,
            "missing aggregate cannot be inserted by save");
        try
        {
            OperationResult.Success(null!);
            throw new InvalidOperationException("Expected result guard.");
        }
        catch (ArgumentNullException) { }
        await CheckResolveAsync();
        Console.WriteLine("PASS: architecture behavior");
    }
    private static async Task CheckResolveAsync()
    {
        var ready = New().ChangeStatus("InProgress")!;
        var store = StoreFactory.Create([ready]);
        var service = new ResolveTicket(store);
        foreach (string? text in new string?[] { null, " ", "ab", new string('x', 501) })
            Check((await service.ExecuteAsync(new(1, text), Ct)).Code == OperationCode.Invalid, "invalid resolution");
        Check((await service.ExecuteAsync(new(99, "Fixed cable"), Ct)).Code == OperationCode.Missing, "resolve missing");
        Check((await store.LoadAsync(1, Ct))?.Version == 1, "validation causes no writes");
        var result = await service.ExecuteAsync(new(1, "  Fixed cable  "), Ct);
        var saved = (await store.LoadAsync(1, Ct))!;
        Check(result.Code == OperationCode.Success && saved.Value.Status == "Closed" &&
              saved.Value.Notes.Single() == "Fixed cable" && saved.Version == 2, "resolve commits once");
        Check((await service.ExecuteAsync(new(1, "Fixed cable"), Ct)).Code == OperationCode.Conflict, "resolve is not idempotent");

        var open = StoreFactory.Create([New()]);
        var original = await open.LoadAsync(1, Ct);
        Check((await new ResolveTicket(open).ExecuteAsync(new(1, "Fixed cable"), Ct)).Code == OperationCode.Conflict,
            "Open cannot transition directly to Closed");
        Check(await open.LoadAsync(1, Ct) == original, "note not saved when later domain step fails");

        var full = ready;
        for (int i = 0; i < 5; i++) full = full.AddNote($"Note {i}")!;
        foreach (var seed in new[] { full, ready.AddNote("Fixed cable")! })
        {
            var blocked = StoreFactory.Create([seed]);
            var before = await blocked.LoadAsync(1, Ct);
            Check((await new ResolveTicket(blocked).ExecuteAsync(new(1, "Fixed cable"), Ct)).Code == OperationCode.Conflict,
                "capacity or duplicate resolution");
            Check(await blocked.LoadAsync(1, Ct) == before, "no partial state");
        }
        foreach (var outcome in new[] { SaveOutcome.Conflict, SaveOutcome.Missing })
        {
            var inner = StoreFactory.Create([ready]);
            var rejecting = new RejectingStore(inner, outcome);
            var before = await inner.LoadAsync(1, Ct);
            var rejected = await new ResolveTicket(rejecting).ExecuteAsync(new(1, "Fixed cable"), Ct);
            Check(rejected.Code == (outcome == SaveOutcome.Missing ? OperationCode.Missing : OperationCode.Conflict),
                "store failure mapped");
            Check(rejecting.Saves == 1 && await inner.LoadAsync(1, Ct) == before, "failed commit leaves original");
        }
        var throwing = new RejectingStore(StoreFactory.Create([ready]), null);
        try
        {
            await new ResolveTicket(throwing).ExecuteAsync(new(1, "Fixed cable"), Ct);
            throw new InvalidOperationException("Expected storage failure.");
        }
        catch (IOException) { }
        Check((await throwing.LoadAsync(1, Ct))?.Value.Notes.Count == 0, "IO failure has no partial write");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        try
        {
            await service.ExecuteAsync(new(1, "Fixed cable"), cancelled.Token);
            throw new InvalidOperationException("Expected resolve cancellation.");
        }
        catch (OperationCanceledException) { }
    }

    private sealed class RejectingStore(ITicketStore inner, SaveOutcome? outcome) : ITicketStore
    {
        public int Saves { get; private set; }
        public Task<TicketSnapshot?> LoadAsync(int id, CancellationToken ct) => inner.LoadAsync(id, ct);
        public Task<SaveOutcome> SaveAsync(TicketSnapshot expected, Ticket changed, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Saves++;
            if (outcome is null) throw new IOException("Simulated unavailable storage.");
            return Task.FromResult(outcome.Value);
        }
    }
}
