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
        
        Console.WriteLine("PASS: architecture behavior");
    }
    
}
