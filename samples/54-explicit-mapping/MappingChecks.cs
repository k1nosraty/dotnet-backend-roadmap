using System.Text.Json;
using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;
using SupportDesk.Presentation;

internal static class MappingChecks
{
    public static async Task RunAsync()
    {
        var seed = Ticket.Create(1, TicketTitle.Create("Network issue")).ChangeStatus("InProgress")!.AddNote("Private detail")!;
        var response = TicketMapping.ToResponse(seed);
        string[] keys = JsonDocument.Parse(JsonSerializer.Serialize(response)).RootElement
            .EnumerateObject().Select(p => p.Name).OrderBy(x => x).ToArray();
        if (!keys.SequenceEqual(new[] { "Id", "NoteCount", "Status", "Title" }))
            throw new Exception("Unexpected response shape.");
        var store = StoreFactory.Create([seed]);
        // Serializer ignores unknown fields by default. They cannot become Domain setters.
        var incoming = JsonSerializer.Deserialize<RenameRequest>(
            "{\"Title\":\"  New title  ",\"Status\":\"Closed\",\"Version\":999,\"Id\":99}")!;
        var command = RequestMapping.ToCommand(1, incoming);
        var result = await new RenameTicket(store).ExecuteAsync(command, CancellationToken.None);
        var changed = (await store.LoadAsync(1, CancellationToken.None))!;
        if (result.Code != OperationCode.Success || result.Ticket?.Title != "New title" ||
            changed.Value.Status != "InProgress" || changed.Version != 2 || changed.Value.Notes.Single() != "Private detail")
            throw new Exception("Mapping bypassed a boundary.");
        var service = new RenameTicket(store);
        if ((await service.ExecuteAsync(new(1, "ab"), CancellationToken.None)).Code != OperationCode.Invalid)
            throw new Exception("Invalid title not rejected.");
        var closed = seed.ChangeStatus("Closed")!;
        var closedStore = StoreFactory.Create([closed]);
        if ((await new RenameTicket(closedStore).ExecuteAsync(new(1, "Other title"), CancellationToken.None)).Code != OperationCode.Conflict)
            throw new Exception("Closed ticket rule bypassed.");
        Console.WriteLine("PASS: explicit mapping and constrained request shape");
    }
}
