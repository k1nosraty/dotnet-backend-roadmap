var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => Results.Ok(new { service = "container-lab", version = "1.0.0" }));
app.MapGet("/health/live", () => Results.Text("Healthy"));
app.Run();
public partial class Program { }
