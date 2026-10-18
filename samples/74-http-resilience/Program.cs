using Microsoft.Extensions.DependencyInjection;
using SupportDesk;
var services = new ServiceCollection();
services.AddLogging();
var transport = new DemoPartnerHandler();
services.AddPartner().ConfigurePrimaryHttpMessageHandler(() => transport);
using var provider = services.BuildServiceProvider();
var status = await provider.GetRequiredService<PartnerClient>().CheckAsync(CancellationToken.None);
Console.WriteLine($"status={(int)status}; attempts={transport.Attempts}");
