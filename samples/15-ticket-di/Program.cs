using Microsoft.Extensions.DependencyInjection;
using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;
using SupportDesk.Lifetimes;

ServiceCollection services = new();
services.AddTransient<TransientProbe>();
services.AddScoped<ScopedProbe>();
services.AddSingleton<SingletonProbe>();
// Read-only demo data. This singleton reader is not a general concurrent mutable store.
services.AddSingleton<ITicketReader>(_ => new InMemoryTicketReader(new[] { new Ticket(1, "Network issue") }));
services.AddTransient<TicketSummaryService>();
using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateScopes = true, ValidateOnBuild = true
});
Console.WriteLine(provider.GetRequiredService<TicketSummaryService>().GetSummary(1));
ScopedProbe captured;
SingletonProbe singleton;
using (IServiceScope first = provider.CreateScope())
{
    captured = first.ServiceProvider.GetRequiredService<ScopedProbe>();
    singleton = first.ServiceProvider.GetRequiredService<SingletonProbe>();
    Console.WriteLine($"Transient same: {ReferenceEquals(first.ServiceProvider.GetRequiredService<TransientProbe>(), first.ServiceProvider.GetRequiredService<TransientProbe>())}");
    Console.WriteLine($"Scoped same inside: {ReferenceEquals(captured, first.ServiceProvider.GetRequiredService<ScopedProbe>())}");
    Console.WriteLine($"Disposed inside: {captured.IsDisposed}");
}
Console.WriteLine($"Disposed after scope: {captured.IsDisposed}");
using (IServiceScope second = provider.CreateScope())
{
    Console.WriteLine($"Scoped same across: {ReferenceEquals(captured, second.ServiceProvider.GetRequiredService<ScopedProbe>())}");
    Console.WriteLine($"Singleton same across: {ReferenceEquals(singleton, second.ServiceProvider.GetRequiredService<SingletonProbe>())}");
}
try { _ = provider.GetRequiredService<ScopedProbe>(); }
catch (InvalidOperationException) { Console.WriteLine("Root scoped resolution rejected."); }

ServiceCollection invalid = new();
invalid.AddScoped<ScopedProbe>();
invalid.AddSingleton<BadSingleton>();
try
{
    using ServiceProvider bad = invalid.BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateScopes = true, ValidateOnBuild = true
    });
    Console.WriteLine("Unexpected: captive dependency accepted.");
}
catch (AggregateException) { Console.WriteLine("Captive scoped dependency rejected at build."); }
