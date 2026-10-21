using System.Reflection;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var commit = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
    .Single(attribute => attribute.Key == "CourseCommit").Value;
app.MapGet("/version", () => Results.Ok(new { service = "release-lab", version = "1.0.0", commit }));
app.MapGet("/health/live", () => Results.Text("Healthy"));
app.Run();
public partial class Program { }
