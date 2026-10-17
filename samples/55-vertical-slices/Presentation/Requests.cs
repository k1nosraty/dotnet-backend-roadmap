using SupportDesk.Application;
namespace SupportDesk.Presentation;

// No status, notes, storage version or other writable aggregate fields.
public sealed record RenameRequest(string? Title);
public sealed record ResolveRequest(string? Resolution);
public static class RequestMapping
{
    public static RenameTicketCommand ToCommand(int routeId, RenameRequest request) => new(routeId, request.Title);
    public static ResolveTicketCommand ToCommand(int routeId, ResolveRequest request) => new(routeId, request.Resolution);
}
