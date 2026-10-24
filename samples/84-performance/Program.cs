using System.Diagnostics;
using SupportDesk;
var builder=WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.AspNetCore",LogLevel.Warning);
var app=builder.Build();
app.MapGet("/health/live",()=>Results.Text("Healthy"));
app.MapGet("/sum/{algorithm}",(string algorithm,int count)=>
{
    if(!Squares.Valid(count) || algorithm is not ("loop" or "formula")) return Results.BadRequest();
    var value=algorithm=="loop" ? Squares.Loop(count) : Squares.Formula(count);
    return Results.Ok(new { value });
});
// Synthetic loopback lab diagnostics, not a public production management endpoint.
app.MapGet("/runtime",()=>
{
    using var process=Process.GetCurrentProcess();
    return Results.Ok(new { cpuMs=process.TotalProcessorTime.TotalMilliseconds, allocatedBytes=GC.GetTotalAllocatedBytes() });
});
app.Run();
public partial class Program { }
