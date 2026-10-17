using System.Collections.Concurrent;
using System.Text.Json;
using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

// Educational single-process file adapter. Not a distributed database.
internal sealed class JsonTicketStore : ITicketStore
{
    // Canonical lexical paths coordinate instances in this process, not symlink aliases.
    private static readonly ConcurrentDictionary<string, object> Gates = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly string path;
    private readonly object gate;

    internal JsonTicketStore(string path, IEnumerable<Ticket> seed)
    {
        this.path = Path.GetFullPath(path);
        gate = Gates.GetOrAdd(this.path, _ => new object());
        lock (gate)
        {
            if (File.Exists(this.path)) { _ = Read(); return; }
            var rows = seed.Select(t => new TicketSnapshot(t, 1)).ToDictionary(t => t.Value.Id);
            Write(rows, CancellationToken.None);
        }
    }

    public Task<TicketSnapshot?> LoadAsync(int id, CancellationToken ct)
    {
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            var rows = Read();
            rows.TryGetValue(id, out var row);
            return Task.FromResult(row);
        }
    }

    public Task<SaveOutcome> SaveAsync(TicketSnapshot expected, Ticket changed, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(changed);
        if (expected.Value.Id != changed.Id)
            throw new ArgumentException("A mutation cannot change identity.", nameof(changed));
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            var rows = Read();
            if (!rows.TryGetValue(changed.Id, out var current)) return Task.FromResult(SaveOutcome.Missing);
            if (current.Version != expected.Version) return Task.FromResult(SaveOutcome.Conflict);
            rows[changed.Id] = new(changed, checked(current.Version + 1));
            Write(rows, ct);
            return Task.FromResult(SaveOutcome.Saved);
        }
    }

    private Dictionary<int, TicketSnapshot> Read()
    {
        try
        {
            var document = JsonSerializer.Deserialize<StoreDocument>(File.ReadAllText(path));
            if (document is null || document.FormatVersion != 1 || document.Tickets is null)
                throw new InvalidDataException("Unsupported or incomplete store document.");
            var rows = new Dictionary<int, TicketSnapshot>();
            foreach (var row in document.Tickets)
            {
                if (row is null) throw new InvalidDataException("Null stored ticket.");
                var snapshot = StorageMapping.ToSnapshot(row);
                if (!rows.TryAdd(snapshot.Value.Id, snapshot))
                    throw new InvalidDataException("Duplicate ticket identity.");
            }
            return rows;
        }
        catch (JsonException ex) { throw new InvalidDataException("Malformed JSON store.", ex); }
    }

    private void Write(Dictionary<int, TicketSnapshot> rows, CancellationToken ct)
    {
        // Same directory avoids an intentional cross-volume copy. No power-loss guarantee is claimed.
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var document = new StoreDocument(1, rows.Values.OrderBy(x => x.Value.Id)
                .Select(StorageMapping.ToRow).ToArray());
            File.WriteAllText(temporary, JsonSerializer.Serialize(document));
            ct.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
