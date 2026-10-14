using System.Text.Json;
using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

public sealed class JsonTicketStore : ITicketStore
{
    private readonly string path;
    public JsonTicketStore(string path) => this.path = Path.GetFullPath(path);
    public async Task<TicketState> LoadAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string json;
        try { json = await File.ReadAllTextAsync(path, token); }
        catch (FileNotFoundException) { return new(); }
        StoreDocument? document = JsonSerializer.Deserialize<StoreDocument>(json);
        if (document is null || document.Schema != "supportdesk-v1" || document.Tickets is null || document.NextId < 1 || document.NextId > (long)int.MaxValue + 1)
            throw new InvalidDataException("Invalid store schema or next ID.");
        TicketState state = new() { NextId = document.NextId };
        HashSet<int> ids = new();
        foreach (TicketRow? row in document.Tickets)
        {
            token.ThrowIfCancellationRequested();
            if (row is null || row.Title is null || row.Notes is null || row.Notes.Any(n => n is null) || !ids.Add(row.Id))
                throw new InvalidDataException("Missing or duplicate data.");
            try { state.Tickets.Add(Ticket.Restore(row.Id, TicketTitle.Create(row.Title), row.Priority, row.Status, row.Notes.Select(n => n!))); }
            catch (ArgumentException ex) { throw new InvalidDataException("Invalid ticket values.", ex); }
            catch (InvalidOperationException ex) { throw new InvalidDataException("Invalid ticket state.", ex); }
        }
        if (state.Tickets.Any(t => t.Id >= state.NextId)) throw new InvalidDataException("Next ID would collide.");
        return state;
    }
    public async Task SaveAsync(TicketState state, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(state);
        if (state.NextId < 1 || state.NextId > (long)int.MaxValue + 1 || state.Tickets.Any(t => t.Id >= state.NextId) || state.Tickets.Select(t => t.Id).Distinct().Count() != state.Tickets.Count)
            throw new InvalidDataException("Invalid state for persistence.");
        StoreDocument document = new()
        {
            Schema = "supportdesk-v1", NextId = state.NextId,
            Tickets = state.Tickets.Select(t => (TicketRow?)new TicketRow
            {
                Id = t.Id, Title = t.Title.Value, Priority = t.Priority, Status = t.Status,
                Notes = t.Notes.Select(n => (string?)n).ToList()
            }).ToList()
        };
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }), token);
            token.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
