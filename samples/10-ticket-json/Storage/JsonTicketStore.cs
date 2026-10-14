using System.Text.Json;
using SupportDesk.Domain;

namespace SupportDesk.Storage;

public sealed class JsonTicketStore
{
    private readonly string path;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public JsonTicketStore(string path) => this.path = Path.GetFullPath(path);

    public List<Ticket> Load()
    {
        string json;
        try { json = File.ReadAllText(path); }
        catch (FileNotFoundException) { return new(); }
        // Missing directory, access denied or malformed files are not an empty store.
        return SnapshotMapper.Import(JsonSerializer.Deserialize<StoreSnapshot>(json, Options));
    }

    public void Save(IEnumerable<Ticket> tickets)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            string json = JsonSerializer.Serialize(SnapshotMapper.Export(tickets), Options);
            File.WriteAllText(temporary, json);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (IOException) { /* Best-effort cleanup; never hide the original error. */ }
            catch (UnauthorizedAccessException) { }
        }
    }
}
