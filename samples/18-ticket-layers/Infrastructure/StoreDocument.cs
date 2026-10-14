namespace SupportDesk.Infrastructure;

public sealed class StoreDocument
{
    public string? Schema { get; init; }
    public List<StoredTicket?>? Items { get; init; }
}
public sealed class StoredTicket
{
    public int Id { get; init; }
    public string? Title { get; init; }
}
