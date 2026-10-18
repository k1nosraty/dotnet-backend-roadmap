using System.Collections.Concurrent;
using System.Text.Json;
using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

// Educational single-process file adapter. Not a distributed database.
internal sealed class JsonTicketStore : ITicketStore, ITicketQueries, ITicketLifecycle
{
    // Canonical lexical paths coordinate instances in this process, not symlink aliases.
    private static readonly ConcurrentDictionary<string, object> Gates = new(
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly string path;
    private long nextId;
    private readonly object gate;

    internal JsonTicketStore(string path, IEnumerable<Ticket> seed)
    {
        this.path = Path.GetFullPath(path);
        gate = Gates.GetOrAdd(this.path, _ => new object());
        lock (gate)
        {
            if (File.Exists(this.path)) { _ = Read(); return; }
            var rows = seed.Select(t => new TicketSnapshot(t, 1)).ToDictionary(t => t.Value.Id);
            nextId = rows.Count == 0 ? 1 : (long)rows.Keys.Max() + 1;
            Write(rows, CancellationToken.None);
        }
    }

    public Task<Ticket?> CreateAsync(TicketTitle title, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(title);
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            var data = Read();
            if (nextId > int.MaxValue) return Task.FromResult<Ticket?>(null);
            var created = Ticket.Create((int)nextId, title);
            data.Add(created.Id, new(created, 1));
            nextId++;
            Write(data, ct);
            return Task.FromResult<Ticket?>(created);
        }
    }
    public Task<SaveOutcome> DeleteAsync(TicketSnapshot expected, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(expected);
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            var data = Read();
            if (!data.TryGetValue(expected.Value.Id, out var current)) return Task.FromResult(SaveOutcome.Missing);
            if (current.Version != expected.Version) return Task.FromResult(SaveOutcome.Conflict);
            data.Remove(expected.Value.Id);
            Write(data, ct);
            return Task.FromResult(SaveOutcome.Saved);
        }
    }

    public Task<TicketResponse?> FindAsync(int id, CancellationToken ct)
    {
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            var data = Read();
            return Task.FromResult(data.TryGetValue(id, out var row) ? TicketMapping.ToResponse(row.Value) : null);
        }
    }
    public Task<TicketPage> ListAsync(TicketListQuery query, CancellationToken ct)
    {
        lock (gate)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(QueryProjection.Page(Read().Values, query));
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
            if (document is null || document.FormatVersion != 2 || document.Tickets is null)
                throw new InvalidDataException("Unsupported or incomplete store document.");
            var rows = new Dictionary<int, TicketSnapshot>();
            foreach (var row in document.Tickets)
            {
                if (row is null) throw new InvalidDataException("Null stored ticket.");
                var snapshot = StorageMapping.ToSnapshot(row);
                if (!rows.TryAdd(snapshot.Value.Id, snapshot))
                    throw new InvalidDataException("Duplicate ticket identity.");
            }
            long maxId = rows.Count == 0 ? 0 : rows.Keys.Max();
            if (document.NextId <= maxId || document.NextId < 1 || document.NextId > (long)int.MaxValue + 1)
                throw new InvalidDataException("Invalid stored identity counter.");
            nextId = document.NextId;
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
            var document = new StoreDocument(2, nextId, rows.Values.OrderBy(x => x.Value.Id)
                .Select(StorageMapping.ToRow).ToArray());
            File.WriteAllText(temporary, JsonSerializer.Serialize(document));
            ct.ThrowIfCancellationRequested();
            // Windows readers (including file scanners) may briefly deny delete sharing.
            // Retry only sharing/lock violations; permission and other I/O errors still fail.
            for (int attempt = 0; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try { File.Move(temporary, path, overwrite: true); break; }
                catch (IOException ex) when (OperatingSystem.IsWindows() && attempt < 20 &&
                    (ex.HResult & 0xffff) is 32 or 33)
                {
                    if (ct.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(25)))
                        ct.ThrowIfCancellationRequested();
                }
            }
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
