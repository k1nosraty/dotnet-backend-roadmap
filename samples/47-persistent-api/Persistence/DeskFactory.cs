using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace SupportDesk.Persistence;
public sealed class DeskFactory : IDesignTimeDbContextFactory<DeskContext>
{
 public DeskContext CreateDbContext(string[] args)
 {
  // Scaffolding needs a syntactically valid connection, not a running database.
  string cs = Environment.GetEnvironmentVariable("SUPPORTDESK_PG")
   ?? "Host=localhost;Database=supportdesk_lab;Username=course";
  return new(new DbContextOptionsBuilder<DeskContext>().UseNpgsql(cs).Options);
 }
}
