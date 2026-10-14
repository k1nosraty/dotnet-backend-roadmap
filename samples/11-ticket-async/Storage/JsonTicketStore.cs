using System.Text.Json;
using SupportDesk.Domain;

namespace SupportDesk.Storage;

public sealed class JsonTicketStore
{
    private readonly string path;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public JsonTicketStore(string path) => this.path = Path.GetFullPath(path);

    public async Task<List<Ticket>> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string json;
        try { json = await File.ReadAllTextAsync(path, cancellationToken); }
        catch (FileNotFoundException) { return new(); }
        cancellationToken.ThrowIfCancellationRequested();
        return SnapshotMapper.Import(JsonSerializer.Deserialize<StoreSnapshot>(json, Options));
    }

    public async Task SaveAsync(IEnumerable<Ticket> tickets, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            string json = JsonSerializer.Serialize(SnapshotMapper.Export(tickets), Options);
            await File.WriteAllTextAsync(temporary, json, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
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
