using Npgsql;
namespace SupportDesk.Infrastructure;

// Infrastructure for disposable teaching schemas; no HTTP or business policy lives here.
public static class Lab
{
    public static async Task<int> RunAsync(string prefix, string[] args,
        Func<NpgsqlDataSource, CancellationToken, Task> action)
    {
        if (args.Any(a => a != "--keep")) { Console.Error.WriteLine("Only --keep is supported."); return 2; }
        string? connectionString = Environment.GetEnvironmentVariable("SUPPORTDESK_PG");
        if (string.IsNullOrWhiteSpace(connectionString))
        { Console.Error.WriteLine("Set SUPPORTDESK_PG for a dedicated PostgreSQL lab database."); return 2; }
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(90));
        // Generated here, never accepted from a caller or concatenated from user text.
        string schema = prefix + "_" + Guid.NewGuid().ToString("N");
        bool created = false;
        bool cleanupFailed = false;
        try
        {
            await using NpgsqlDataSource control = NpgsqlDataSource.Create(connectionString);
            try
            {
                await ExecuteAsync(control, $"CREATE SCHEMA {schema}", timeout.Token);
                created = true;
                Console.WriteLine($"Lab schema: {schema}");
                var settings = new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = schema };
                await using NpgsqlDataSource db = NpgsqlDataSource.Create(settings.ConnectionString);
                await action(db, timeout.Token);
                Console.WriteLine("PASS: lesson scenario");
            }
            finally
            {
                if (created && !args.Contains("--keep"))
                {
                    try { await ExecuteAsync(control, $"DROP SCHEMA {schema} CASCADE", CancellationToken.None); }
                    catch (NpgsqlException) { cleanupFailed = true; Console.Error.WriteLine("Lab cleanup failed; inspect the generated schema."); }
                }
                else if (created) Console.WriteLine("Schema retained for inspection (--keep).");
            }
        }
        catch (PostgresException ex)
        { Console.Error.WriteLine($"PostgreSQL rejected the operation; SQLSTATE={ex.SqlState}."); return 1; }
        catch (NpgsqlException)
        { Console.Error.WriteLine("Database operation failed; check server, connection and access."); return 1; }
        catch (OperationCanceledException)
        { Console.Error.WriteLine("Lab timed out; inspect locks and server availability."); return 1; }
        catch (Exception ex)
        { Console.Error.WriteLine($"Scenario failed: {ex.GetType().Name}."); return 1; }
        return cleanupFailed ? 1 : 0;
    }
    public static async Task ExecuteAsync(NpgsqlDataSource db, string sql, CancellationToken token)
    {
        await using var command = db.CreateCommand(sql);
        command.CommandTimeout = 20;
        await command.ExecuteNonQueryAsync(token);
    }
    public static async Task ScriptAsync(NpgsqlDataSource db, string file, CancellationToken token)
    {
        string sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", file), token);
        await ExecuteAsync(db, sql, token);
    }
    public static async Task PrintAsync(NpgsqlDataSource db, string file, CancellationToken token)
    {
        string sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Sql", file), token);
        await using var command = db.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(token);
        do
        {
            Console.WriteLine(string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(reader.GetName)));
            while (await reader.ReadAsync(token))
                Console.WriteLine(string.Join(" | ", Enumerable.Range(0, reader.FieldCount)
                    .Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));
        } while (await reader.NextResultAsync(token));
    }
    public static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
