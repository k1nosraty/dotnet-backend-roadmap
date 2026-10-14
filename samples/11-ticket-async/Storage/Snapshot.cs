using SupportDesk.Domain;

namespace SupportDesk.Storage;

// A storage contract, not an API contract and not the domain entity.
public sealed class TicketSnapshot
{
    public int Id { get; init; }
    public string? Title { get; init; }
    public TicketStatus Status { get; init; }
}

public sealed class StoreSnapshot
{
    public int Version { get; init; }
    public List<TicketSnapshot?>? Tickets { get; init; }
}

public static class SnapshotMapper
{
    public static StoreSnapshot Export(IEnumerable<Ticket> tickets) => new()
    {
        Version = 1,
        Tickets = tickets.Select(t => (TicketSnapshot?)new TicketSnapshot
        {
            Id = t.Id, Title = t.Title, Status = t.Status
        }).ToList()
    };

    public static List<Ticket> Import(StoreSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Version != 1 || snapshot.Tickets is null)
            throw new InvalidDataException("Unsupported or incomplete store.");
        List<Ticket> result = new();
        HashSet<int> ids = new();
        foreach (TicketSnapshot? item in snapshot.Tickets)
        {
            if (item is null || item.Title is null || !Enum.IsDefined(item.Status))
                throw new InvalidDataException("Invalid ticket record.");
            if (!ids.Add(item.Id)) throw new InvalidDataException("Duplicate ticket ID.");
            try
            {
                Ticket ticket = new(item.Id, item.Title);
                if (item.Status is TicketStatus.InProgress or TicketStatus.Closed) ticket.Start();
                if (item.Status == TicketStatus.Closed) ticket.Close();
                result.Add(ticket);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException("Invalid ticket values.", ex);
            }
        }
        return result;
    }
}
