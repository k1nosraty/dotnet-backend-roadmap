using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;

internal static class StorageMapping
{
    public static TicketSnapshot ToSnapshot(StoredTicket row)
    {
        if (row.Title is null || row.Status is null || row.Notes is null || row.Version <= 0)
            throw new InvalidDataException("Incomplete stored ticket.");
        try
        {
            return new(Ticket.Restore(row.Id, row.Title, row.Status, row.Notes), row.Version);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException("Invalid stored ticket.", ex);
        }
    }
    public static StoredTicket ToRow(TicketSnapshot snapshot) => new(
        snapshot.Value.Id, snapshot.Value.Title, snapshot.Value.Status,
        snapshot.Value.Notes.ToArray(), snapshot.Version);
}
