using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
namespace SupportDesk.Persistence;
public sealed class CommandCounter : DbCommandInterceptor
{
 public int Reads { get; set; }
 public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
  CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
 { Reads++; return ValueTask.FromResult(result); }
}
