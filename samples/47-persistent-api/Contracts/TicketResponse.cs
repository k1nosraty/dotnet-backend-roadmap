namespace SupportDesk.Contracts;
public sealed record TicketResponse(int Id, string Title, string Status, IReadOnlyList<string> Notes);
