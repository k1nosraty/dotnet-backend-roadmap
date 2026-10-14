using SupportDesk.Domain;
using System.Text.Json.Serialization;
namespace SupportDesk.Infrastructure;

public sealed class StoreDocument
{
    [JsonRequired]
    public string? Schema { get; init; }
    [JsonRequired]
    public long NextId { get; init; }
    [JsonRequired]
    public List<TicketRow?>? Tickets { get; init; }
}
public sealed class TicketRow
{
    [JsonRequired]
    public int Id { get; init; }
    [JsonRequired]
    public string? Title { get; init; }
    [JsonRequired]
    public TicketPriority Priority { get; init; }
    [JsonRequired]
    public TicketStatus Status { get; init; }
    [JsonRequired]
    public List<string?>? Notes { get; init; }
}
