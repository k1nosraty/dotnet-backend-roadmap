using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

internal static class DeliveryChecks
{
    public static async Task RunAsync()
    {
        var ports = StoreFactory.CreateServices([]);
        var create = new CreateTicket(ports.Lifecycle);
        var first = await create.ExecuteAsync(new("Network issue"), CancellationToken.None);
        int id = first.Ticket!.Id;
        var original = (await ports.Commands.LoadAsync(id, CancellationToken.None))!;
        await new RenameTicket(ports.Commands).ExecuteAsync(new(id,"New title"),CancellationToken.None);
        if (await ports.Lifecycle.DeleteAsync(original,CancellationToken.None) != SaveOutcome.Conflict)
            throw new Exception("Stale delete bypassed concurrency.");
        if ((await new DeleteTicket(ports.Commands,ports.Lifecycle).ExecuteAsync(id,CancellationToken.None)).Code != OperationCode.Success)
            throw new Exception("Delete failed.");
        var second = await create.ExecuteAsync(new("Another ticket"),CancellationToken.None);
        if (second.Ticket!.Id <= id) throw new Exception("Deleted identity reused.");
        var invalid = await create.ExecuteAsync(new("ab"),CancellationToken.None);
        if (invalid.Code != OperationCode.Invalid) throw new Exception("Invalid create.");
        var exhausted = StoreFactory.CreateServices([Ticket.Create(int.MaxValue, TicketTitle.Create("Last identity"))]);
        if ((await new CreateTicket(exhausted.Lifecycle).ExecuteAsync(new("No identity left"), CancellationToken.None)).Code != OperationCode.Conflict)
            throw new Exception("Exhausted identity counter accepted a create.");
        Console.WriteLine("PASS: TeamDesk lifecycle, stale delete and monotonic identities");
    }
}
