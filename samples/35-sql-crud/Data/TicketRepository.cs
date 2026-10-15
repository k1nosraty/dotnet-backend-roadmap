using Npgsql;
using NpgsqlTypes;
using SupportDesk.Contracts;
using SupportDesk.Domain;
namespace SupportDesk.Data;

public sealed class TicketRepository(NpgsqlDataSource db)
{
    public async Task<TicketRow> CreateAsync(TicketTitle title, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(title);
        await using var command = db.CreateCommand(
            "INSERT INTO tickets(title) VALUES ($1) RETURNING id,title,status");
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, Value = title.Value });
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) throw new InvalidOperationException("INSERT returned no row.");
        return new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
    }
    public async Task<TicketRow?> FindAsync(int id, CancellationToken token)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        await using var command = db.CreateCommand("SELECT id,title,status FROM tickets WHERE id=$1");
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType=NpgsqlDbType.Integer, Value=id });
        await using var reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)) : null;
    }
    public async Task<bool> RenameAsync(int id, TicketTitle title, CancellationToken token)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        ArgumentNullException.ThrowIfNull(title);
        await using var command = db.CreateCommand("UPDATE tickets SET title=$1 WHERE id=$2");
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType=NpgsqlDbType.Text, Value=title.Value });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType=NpgsqlDbType.Integer, Value=id });
        return await command.ExecuteNonQueryAsync(token) == 1;
    }
    public async Task<bool> DeleteAsync(int id, CancellationToken token)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        await using var command = db.CreateCommand("DELETE FROM tickets WHERE id=$1");
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType=NpgsqlDbType.Integer, Value=id });
        return await command.ExecuteNonQueryAsync(token) == 1;
    }
}
