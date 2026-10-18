using System.Text.Json;
using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

internal static class AdapterChecks
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
    }
    public static async Task RunAsync()
    {
        string folder = Path.Combine(Path.GetTempPath(), "ports-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            Ticket seed = Ticket.Create(1, TicketTitle.Create("Network issue")).ChangeStatus("InProgress")!;
            foreach (string adapter in new[] { "memory", "json" })
            {
                string path = Path.Combine(folder, adapter + ".json");
                ITicketStore store = adapter == "memory" ? StoreFactory.Create([seed]) : StoreFactory.CreateFile(path, [seed]);
                Check(await store.LoadAsync(999, Ct) is null, "missing load");
                var absent = Ticket.Create(999, TicketTitle.Create("Missing ticket"));
                Check(await store.SaveAsync(new TicketSnapshot(absent, 1), absent, Ct) == SaveOutcome.Missing,
                    "save cannot insert a missing aggregate");
                var old = (await store.LoadAsync(1, Ct))!;
                var result = await new ResolveTicket(store).ExecuteAsync(new(1, "اتصال اصلاح شد"), Ct);
                var saved = (await store.LoadAsync(1, Ct))!;
                Check(result.Code == OperationCode.Success && saved.Value.Status == "Closed" &&
                    saved.Value.Notes.Single() == "اتصال اصلاح شد" && saved.Version == 2, "same use case with both adapters");
                Check(await store.SaveAsync(old, old.Value, Ct) == SaveOutcome.Conflict, "stale version");
                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();
                try { await store.SaveAsync(saved, saved.Value, cancelled.Token); throw new Exception("Expected cancellation."); }
                catch (OperationCanceledException) { }
                Check((await store.LoadAsync(1, Ct))!.Version == 2, "cancelled commit");
                try { await store.SaveAsync(saved, Ticket.Create(2, TicketTitle.Create("Other ticket")), Ct); throw new Exception("Expected identity failure."); }
                catch (ArgumentException) { }
                if (adapter == "json")
                {
                    var reopened = StoreFactory.CreateFile(path, []);
                    Check((await reopened.LoadAsync(1, Ct))!.Value.Status == "Closed", "reopen preserves persisted state");
                }
            }

            string racePath = Path.Combine(folder, "race.json");
            var first = StoreFactory.CreateFile(racePath, [seed]);
            var second = StoreFactory.CreateFile(racePath, []);
            var snapshot = (await first.LoadAsync(1, Ct))!;
            var outcomes = await Task.WhenAll(
                Task.Run(() => first.SaveAsync(snapshot, snapshot.Value.AddNote("First writer")!, Ct)),
                Task.Run(() => second.SaveAsync(snapshot, snapshot.Value.AddNote("Second writer")!, Ct)));
            Check(outcomes.Count(x => x == SaveOutcome.Saved) == 1 &&
                  outcomes.Count(x => x == SaveOutcome.Conflict) == 1, "two instances coordinate in one process");

            if (OperatingSystem.IsWindows())
            {
                string sharedPath = Path.Combine(folder, "shared.json");
                var shared = StoreFactory.CreateFile(sharedPath, [seed]);
                var before = (await shared.LoadAsync(1, Ct))!;
                // Permit reads but deliberately prevent replacement until the reader closes.
                var reader = new FileStream(sharedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var release = new Thread(() => { Thread.Sleep(100); reader.Dispose(); });
                release.Start();
                try
                {
                    Check(await shared.SaveAsync(before, before.Value.AddNote("After reader")!, Ct) == SaveOutcome.Saved,
                        "temporary Windows sharing conflict is retried");
                }
                finally { release.Join(); reader.Dispose(); }
                Check((await shared.LoadAsync(1, Ct))!.Version == 2, "retried replacement persists once");
                byte[] preserved = File.ReadAllBytes(sharedPath);
                using (var held = new FileStream(sharedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var current = (await shared.LoadAsync(1, Ct))!;
                    try { await shared.SaveAsync(current, current.Value, Ct); throw new Exception("Expected sharing failure."); }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
                }
                Check(File.ReadAllBytes(sharedPath).SequenceEqual(preserved), "persistent sharing conflict preserves original");
            }

            string broken = Path.Combine(folder, "broken.json");
            var holding = StoreFactory.CreateFile(broken, [seed]);
            var expected = (await holding.LoadAsync(1, Ct))!;
            string[] invalidDocuments = [
                "{broken", "null", "{\"FormatVersion\":2,\"Tickets\":[]}",
                JsonSerializer.Serialize(new { FormatVersion = 2, NextId = 2, Tickets = new object?[] { null } }),
                Document(1, "Closed", []), Document(0, "Open", []), Document(1, "Open", ["Same", "Same"]),
                Document(1, "Deleted", []), Document(1, "Open", [], 0),
                CounterDocument(3, 2), CounterDocument(2, 0), CounterDocument(2, -1),
                CounterDocument(2, 1), CounterDocument(2, (long)int.MaxValue + 2),
                JsonSerializer.Serialize(new { FormatVersion = 2, NextId = 2, Tickets = new[] {
                    new { Id = 1, Title = "Network", Status = "Open", Notes = Array.Empty<string>(), Version = 1 },
                    new { Id = 1, Title = "Network", Status = "Open", Notes = Array.Empty<string>(), Version = 1 } } })
            ];
            foreach (string invalid in invalidDocuments)
            {
                File.WriteAllText(broken, invalid);
                try { StoreFactory.CreateFile(broken, [seed]); throw new Exception("Expected corrupted document rejection."); }
                catch (InvalidDataException) { }
                try { await holding.SaveAsync(expected, expected.Value, Ct); throw new Exception("Expected failed save."); }
                catch (InvalidDataException) { }
                Check(File.ReadAllText(broken) == invalid, "corrupt file is never replaced by seed or save");
            }
            Check(!Directory.EnumerateFiles(folder, "*.tmp").Any(), "temporary files cleaned");
            Console.WriteLine("PASS: port contracts, persisted adapter and corrupt-file preservation");
        }
        finally { Directory.Delete(folder, recursive: true); }
    }
    private static string CounterDocument(int format, long nextId) =>
        JsonSerializer.Serialize(new { FormatVersion = format, NextId = nextId, Tickets = new[] {
            new { Id = 1, Title = "Network", Status = "Open", Notes = Array.Empty<string>(), Version = 1 } } });

    private static string Document(int id, string status, string[] notes, long version = 1) =>
        JsonSerializer.Serialize(new { FormatVersion = 2, NextId = 2, Tickets = new[] {
            new { Id = id, Title = "Network", Status = status, Notes = notes, Version = version } } });
}
