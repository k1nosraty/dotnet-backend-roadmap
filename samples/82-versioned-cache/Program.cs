using StackExchange.Redis;
using SupportDesk;
var source = new TicketSource();
var tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
source.Write(tenant,id,"Printer");
ConnectionMultiplexer? connection = null;
try
{
    ICache cache = new MemoryCache(TimeProvider.System);
    if (Environment.GetEnvironmentVariable("SUPPORTDESK_REDIS") is string address)
    {
        var options = ConfigurationOptions.Parse(address);
        options.AbortOnConnectFail = false; options.ConnectTimeout = 1000; options.AsyncTimeout = 1000;
        connection = await ConnectionMultiplexer.ConnectAsync(options); cache = new RedisCache(connection);
    }
    var builds = 0;
    var service = new CachedSummary(source,cache,(ticket,ct) =>
    { ct.ThrowIfCancellationRequested(); builds++; return Task.FromResult(new Summary(ticket.Version,ticket.Title.ToUpperInvariant())); });
    var first = await service.ReadAsync(tenant,id);
    var second = await service.ReadAsync(tenant,id);
    await service.UpdateAsync(tenant,id,"Network");
    var updated = await service.ReadAsync(tenant,id);
    Console.WriteLine($"first={first!.Label}; second={second!.Label}; updated={updated!.Label}; builds={builds}");
}
finally { if (connection is not null) await connection.DisposeAsync(); }
