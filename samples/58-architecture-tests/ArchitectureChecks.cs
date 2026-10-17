using System.Reflection;
using SupportDesk.Application;
using SupportDesk.Domain;
using SupportDesk.Infrastructure;

internal static class ArchitectureChecks
{
    public static void Run()
    {
        Check(typeof(Ticket).Assembly, []);
        Check(typeof(ResolveTicket).Assembly, ["SupportDesk.Domain"]);
        Check(typeof(StoreFactory).Assembly, ["SupportDesk.Application", "SupportDesk.Domain"]);
        if (typeof(Ticket).Assembly.GetReferencedAssemblies().Any(n => n.Name!.StartsWith("FluentValidation",StringComparison.Ordinal)))
            throw new Exception("Validation package leaked into Domain.");
        if (typeof(StoreFactory).Assembly.GetExportedTypes().Any(t => t.Name is "JsonTicketStore" or "InMemoryTicketStore"))
            throw new Exception("Adapter implementation leaked through public types.");
        Console.WriteLine("PASS: compiled assembly dependency boundaries");
    }
    private static void Check(Assembly assembly, string[] expected)
    {
        string[] actual = assembly.GetReferencedAssemblies().Select(n => n.Name!)
            .Where(n => n.StartsWith("SupportDesk.", StringComparison.Ordinal)).OrderBy(n => n).ToArray();
        if (!actual.SequenceEqual(expected.OrderBy(n => n))) throw new Exception("Unexpected project dependency: " + assembly.GetName().Name);
        string[] forbidden = ["Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Npgsql"];
        if (assembly.GetReferencedAssemblies().Any(n => forbidden.Any(p => n.Name!.StartsWith(p, StringComparison.Ordinal))))
            throw new Exception("Framework detail in a core or adapter assembly.");
    }
}
