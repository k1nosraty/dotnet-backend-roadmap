using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

internal static class QueryChecks
{
    public static async Task RunAsync()
    {
        var seeds = Enumerable.Range(1, 5).Select(i => Ticket.Create(i, TicketTitle.Create($"Network {i}"))).ToArray();
        var ports = StoreFactory.CreateServices(seeds);
        var list = new ListTickets(ports.Queries);
        var original = (await ports.Commands.LoadAsync(1, CancellationToken.None))!;
        var page = (await list.ExecuteAsync(new(Page: 2, PageSize: 2), CancellationToken.None))!;
        if (page.Total != 5 || !page.Items.Select(x => x.Id).SequenceEqual(new[] { 3, 4 }))
            throw new Exception("Pagination order/count.");
        var far = (await list.ExecuteAsync(new(Page: int.MaxValue, PageSize: 50), CancellationToken.None))!;
        if (far.Total != 5 || far.Items.Length != 0) throw new Exception("Page overflow.");
        foreach (var invalid in new[] { new TicketListQuery(Page:0), new TicketListQuery(PageSize:51), new TicketListQuery(Status:"Unknown") })
            if (await list.ExecuteAsync(invalid, CancellationToken.None) is not null) throw new Exception("Invalid query accepted.");
        var found = (await list.ExecuteAsync(new(Q:"  NETWORK 3  ", Status:"Open"), CancellationToken.None))!;
        if (found.Total != 1 || found.Items.Single().Id != 3) throw new Exception("Filter.");
        if (await new GetTicket(ports.Queries).ExecuteAsync(999, CancellationToken.None) is not null)
            throw new Exception("Missing query.");
        if (await ports.Commands.LoadAsync(1, CancellationToken.None) != original)
            throw new Exception("Query changed write state.");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        try { await list.ExecuteAsync(new(), cancellation.Token); throw new Exception("Expected cancelled query."); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: command/query separation, stable pagination and pure reads");
    }
}
