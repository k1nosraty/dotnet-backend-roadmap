using Npgsql;
namespace SupportDesk.Hosting;
public static class DatabaseFailure
{
    public static bool IsUnavailable(Exception exception)
    {
        // EF may wrap transient connection failures in InvalidOperationException.
        // Inspect typed causes; do not classify every InvalidOperationException as an outage.
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
        {
            if (cause is PostgresException postgres)
                return postgres.IsTransient || postgres.SqlState is "42P01" or "42703" or "3F000";
            if (cause is NpgsqlException or TimeoutException) return true;
        }
        return false;
    }
}
