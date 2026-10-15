using System.Data;
using Npgsql;
using SupportDesk.Infrastructure;
return await Lab.RunAsync("lab39", args, async (db, token) =>
{
    await Lab.ScriptAsync(db, "01-schema.sql", token);
    await using var first = await db.OpenConnectionAsync(token);
    await using var second = await db.OpenConnectionAsync(token);
    async Task<string> Read(NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        await using var command = new NpgsqlCommand("SELECT title FROM tickets WHERE id=1", connection, transaction);
        return (string)(await command.ExecuteScalarAsync(token) ?? throw new InvalidOperationException("Missing row."));
    }
    async Task Write(string title)
    {
        await using var command = new NpgsqlCommand("UPDATE tickets SET title=$1 WHERE id=1", second);
        command.Parameters.AddWithValue(title);
        await command.ExecuteNonQueryAsync(token);
    }
    await using (var transaction = await first.BeginTransactionAsync(IsolationLevel.ReadCommitted, token))
    {
        Lab.Check(await Read(first, transaction) == "Original title", "Initial value changed.");
        await Write("Committed elsewhere");
        Lab.Check(await Read(first, transaction) == "Committed elsewhere", "Read Committed snapshot not refreshed.");
        await transaction.CommitAsync(token);
    }
    await using (var transaction = await first.BeginTransactionAsync(IsolationLevel.RepeatableRead, token))
    {
        Lab.Check(await Read(first, transaction) == "Committed elsewhere", "Initial repeatable read.");
        await Write("Newer commit");
        Lab.Check(await Read(first, transaction) == "Committed elsewhere", "Repeatable Read snapshot changed.");
        // Updating a row changed after the snapshot produces serialization failure (40001).
        try
        {
            await using var conflict = new NpgsqlCommand("UPDATE tickets SET title='Conflicting write' WHERE id=1", first, transaction);
            await conflict.ExecuteNonQueryAsync(token);
            throw new InvalidOperationException("Expected serialization failure.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        { await transaction.RollbackAsync(token); }
    }
    await using (var transaction = await first.BeginTransactionAsync(token))
    {
        await using var insert = new NpgsqlCommand("INSERT INTO tickets(title) VALUES ('Rolled back ticket') RETURNING id", first, transaction);
        int id = (int)(await insert.ExecuteScalarAsync(token) ?? throw new InvalidOperationException());
        try
        {
            await using var invalid = new NpgsqlCommand("INSERT INTO notes(ticket_id,body) VALUES (2147483647,'Invalid parent')", first, transaction);
            await invalid.ExecuteNonQueryAsync(token);
            throw new InvalidOperationException("Expected foreign key failure.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        { await transaction.RollbackAsync(token); }
        await using var verify = db.CreateCommand("SELECT count(*) FROM tickets WHERE id=$1");
        verify.Parameters.AddWithValue(id);
        Lab.Check((long)(await verify.ExecuteScalarAsync(token) ?? -1L) == 0, "Partial write survived rollback.");
    }
    await LockDemo.ObserveAsync(db, first, second, token);
    Console.WriteLine("PASS: two sessions, Read Committed, Repeatable Read, SQLSTATE 40001 and rollback; observed row lock.");
});
