using Npgsql;
using SupportDesk;
if(args.Length!=1 || args[0] is not ("init" or "seed" or "once" or "status"))
{ Console.Error.WriteLine("Usage: init|seed|once|status; set SUPPORTDESK_JOBS for a disposable lab database."); return 2; }
var address=Environment.GetEnvironmentVariable("SUPPORTDESK_JOBS");
if(string.IsNullOrWhiteSpace(address)) { Console.Error.WriteLine("Set SUPPORTDESK_JOBS; connection details are never printed."); return 2; }
try
{
    await using var data=NpgsqlDataSource.Create(address);
    var store=new QueueStore(data);
    switch(args[0])
    {
        case "init": await store.InitializeAsync(); Console.WriteLine("schema=ready"); break;
        case "seed": await store.EnqueueAsync(Guid.Parse("11111111-1111-1111-1111-111111111111")); Console.WriteLine("job=accepted"); break;
        case "once":
            var lease=await store.ClaimAsync();
            Console.WriteLine(lease is null ? "job=none" : await store.CompleteAsync(lease) ? "job=done" : "job=lease-lost"); break;
        case "status":
            var count=await store.CountsAsync();
            Console.WriteLine($"pending={count.Pending}; leased={count.Leased}; done={count.Done}; dead={count.Dead}; effects={count.Effects}"); break;
    }
    return 0;
}
catch(Exception ex) when(ex is NpgsqlException or ArgumentException)
{ Console.Error.WriteLine("Queue operation failed; check configuration and database availability."); return 1; }
