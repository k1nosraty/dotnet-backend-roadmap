using System.Text.Json;
using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

// Single process, small files, one writer; this store is not a concurrent database.
public sealed class JsonTicketStore : ITicketCreator, ITicketReader
{
    private readonly string path;
    public JsonTicketStore(string path) => this.path = Path.GetFullPath(path);
    public Ticket Create(TicketTitle title)
    {
        ArgumentNullException.ThrowIfNull(title);
        List<Ticket> tickets = Load();
        int maximum = tickets.Count == 0 ? 0 : tickets.Max(t => t.Id);
        if (maximum == int.MaxValue) throw new InvalidDataException("ID capacity reached.");
        Ticket ticket = new(maximum + 1, title);
        tickets.Add(ticket);
        Save(tickets);
        return ticket;
    }
    public Ticket? FindById(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        return Load().FirstOrDefault(t => t.Id == id);
    }
    private List<Ticket> Load()
    {
        string json;
        try { json = File.ReadAllText(path); }
        catch (FileNotFoundException) { return new(); }
        StoreDocument? document = JsonSerializer.Deserialize<StoreDocument>(json);
        if (document is null || document.Schema != "registration-v1" || document.Items is null)
            throw new InvalidDataException("Unknown storage schema.");
        List<Ticket> result = new();
        HashSet<int> ids = new();
        foreach (StoredTicket? row in document.Items)
        {
            if (row is null || row.Title is null || !ids.Add(row.Id))
                throw new InvalidDataException("Missing or duplicate record.");
            try { result.Add(new Ticket(row.Id, TicketTitle.Create(row.Title))); }
            catch (ArgumentException ex) { throw new InvalidDataException("Invalid stored values.", ex); }
        }
        return result;
    }
    private void Save(IEnumerable<Ticket> tickets)
    {
        StoreDocument document = new()
        {
            Schema = "registration-v1",
            Items = tickets.Select(t => (StoredTicket?)new StoredTicket { Id = t.Id, Title = t.Title.Value }).ToList()
        };
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(document));
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
