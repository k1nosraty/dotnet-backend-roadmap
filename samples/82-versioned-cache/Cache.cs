using System.Text.Json;
using StackExchange.Redis;
namespace SupportDesk;
public sealed record Ticket(Guid Tenant, Guid Id, long Version, string Title);
public sealed record Summary(long Version, string Label);
public interface ICache
{
    Task<string?> GetAsync(string key, CancellationToken ct);
    Task PutAsync(string key, string value, TimeSpan ttl, CancellationToken ct);
    Task RemoveAsync(string key, CancellationToken ct);
}
public sealed class TicketSource
{
    private readonly object gate = new();
    private readonly Dictionary<(Guid, Guid), Ticket> rows = new();
    public Ticket? Read(Guid tenant, Guid id) { lock (gate) return rows.GetValueOrDefault((tenant,id)); }
    public Ticket Write(Guid tenant, Guid id, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        lock (gate)
        {
            var ticket = new Ticket(tenant, id, rows.GetValueOrDefault((tenant,id))?.Version + 1 ?? 1, title);
            rows[(tenant,id)] = ticket; return ticket;
        }
    }
}
public sealed class CachedSummary(TicketSource source, ICache cache, Func<Ticket, CancellationToken, Task<Summary>> project)
{
    public static string Key(Ticket ticket) => $"summary:v1:{ticket.Tenant:N}:{ticket.Id:N}:{ticket.Version}";
    public async Task<Summary?> ReadAsync(Guid tenant, Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var ticket = source.Read(tenant, id); // Authoritative revision is still read on every request.
        if (ticket is null) return null;
        try
        {
            var text = await cache.GetAsync(Key(ticket), ct);
            if (text is not null)
            {
                try { var value = JsonSerializer.Deserialize<Summary>(text); if (value?.Version == ticket.Version) return value; }
                catch (JsonException) { } // Broken cache is a miss; authoritative state is untouched.
            }
        }
        catch (RedisException) { } // A cache outage may reduce speed, never replace source truth.
        var summary = await project(ticket, ct);
        try { await cache.PutAsync(Key(ticket), JsonSerializer.Serialize(summary), TimeSpan.FromSeconds(30), ct); }
        catch (RedisException) { }
        return summary;
    }
    public async Task UpdateAsync(Guid tenant, Guid id, string title, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var previous = source.Read(tenant,id);
        source.Write(tenant,id,title); // Commit the authoritative change BEFORE best-effort invalidation.
        if (previous is not null)
            try { await cache.RemoveAsync(Key(previous), ct); } catch (RedisException) { }
    }
}
public sealed class MemoryCache(TimeProvider clock) : ICache
{
    private readonly object gate = new();
    private readonly Dictionary<string,(string Value, DateTimeOffset Until)> items = new();
    public Task<string?> GetAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (gate) return Task.FromResult(items.TryGetValue(key,out var item) && item.Until > clock.GetUtcNow() ? item.Value : null);
    }
    public Task PutAsync(string key, string value, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock(gate)
        {
            // shortcut: bounded FIFO for this console lab; use a managed cache eviction policy in a server.
            if(items.Count>=128 && !items.ContainsKey(key)) items.Remove(items.Keys.First());
            items[key]=(value,clock.GetUtcNow()+ttl);
        }
        return Task.CompletedTask;
    }
    public Task RemoveAsync(string key, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); lock(gate) items.Remove(key); return Task.CompletedTask; }
}
public sealed class RedisCache(IConnectionMultiplexer connection) : ICache
{
    private readonly IDatabase database = connection.GetDatabase();
    public async Task<string?> GetAsync(string key, CancellationToken ct)
    { var value = await database.StringGetAsync(key).WaitAsync(ct); return value.IsNull ? null : value.ToString(); }
    public async Task PutAsync(string key, string value, TimeSpan ttl, CancellationToken ct)
        => await database.StringSetAsync(key,value,ttl).WaitAsync(ct);
    public async Task RemoveAsync(string key, CancellationToken ct) => await database.KeyDeleteAsync(key).WaitAsync(ct);
}
