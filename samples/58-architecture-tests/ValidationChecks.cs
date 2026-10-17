using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

internal static class ValidationChecks
{
    public static async Task RunAsync()
    {
        var validator = new ResolveTicketValidator();
        foreach (string? text in new string?[] { null, " ", "ab", new string('x', 501) })
            if ((await validator.ValidateAsync(new ResolveTicketCommand(1, text))).IsValid) throw new Exception("Invalid resolution accepted.");
        if (!(await validator.ValidateAsync(new ResolveTicketCommand(1, "  abc  "))).IsValid ||
            !(await validator.ValidateAsync(new ResolveTicketCommand(1, new string('x',500)))).IsValid)
            throw new Exception("Valid boundary rejected.");
        var multiple = await validator.ValidateAsync(new ResolveTicketCommand(0, null));
        if (multiple.Errors.Count != 2) throw new Exception("Expected both field errors.");
        var ticket = Ticket.Create(1, TicketTitle.Create("Network issue")).ChangeStatus("InProgress")!;
        var store = StoreFactory.Create([ticket]);
        var before = await store.LoadAsync(1, CancellationToken.None);
        if ((await new ResolveTicket(store).ExecuteAsync(new(1,"ab"),CancellationToken.None)).Code != OperationCode.Invalid ||
            await store.LoadAsync(1,CancellationToken.None) != before) throw new Exception("Invalid command wrote state.");
        var closed = ticket.AddNote("Fixed cable")!.ChangeStatus("Closed")!;
        if (!(await validator.ValidateAsync(new ResolveTicketCommand(1,"Another valid note"))).IsValid ||
            closed.AddNote("Another valid note") is not null) throw new Exception("Validation replaced Domain invariant.");
        Console.WriteLine("PASS: FluentValidation boundaries and preserved Domain rules");
    }
}
