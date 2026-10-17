namespace SupportDesk.Infrastructure;

internal sealed record StoreDocument(int FormatVersion, StoredTicket[]? Tickets);
internal sealed record StoredTicket(int Id, string? Title, string? Status, string[]? Notes, long Version);
