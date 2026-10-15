using Npgsql;
namespace SupportDesk.Infrastructure;

public static class LockDemo
{
    public static async Task ObserveAsync(NpgsqlDataSource db, NpgsqlConnection first,
        NpgsqlConnection second, CancellationToken token)
    {
        await using var owner = await first.BeginTransactionAsync(token);
        await using (var hold = new NpgsqlCommand("SELECT id FROM tickets WHERE id=1 FOR UPDATE", first, owner))
            await hold.ExecuteScalarAsync(token);
        await using var contender = await second.BeginTransactionAsync(token);
        await using var update = new NpgsqlCommand("UPDATE tickets SET title='After lock' WHERE id=1", second, contender);
        Task<int> pending = update.ExecuteNonQueryAsync(token);
        bool observed = false;
        try
        {
            // Observe an actual wait, instead of assuming a fixed sleep proves blocking.
            for (int attempt = 0; attempt < 200; attempt++)
            {
                await using var probe = db.CreateCommand("SELECT cardinality(pg_blocking_pids($1)) > 0");
                probe.Parameters.AddWithValue(second.ProcessID);
                observed = (bool)(await probe.ExecuteScalarAsync(token) ?? false);
                if (observed || pending.IsCompleted) break;
                await Task.Delay(25, token);
            }
        }
        finally
        {
            // Release before inspecting assertions, so a failing test cannot strand the contender.
            await owner.RollbackAsync(CancellationToken.None);
        }
        int changed = await pending;
        await contender.CommitAsync(token);
        Lab.Check(observed && changed == 1, "Expected a real row-lock wait.");
    }
}
