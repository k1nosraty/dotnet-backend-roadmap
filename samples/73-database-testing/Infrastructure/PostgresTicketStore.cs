using System.Text.RegularExpressions;
using Npgsql;
using SupportDesk.Application;
using SupportDesk.Domain;
namespace SupportDesk.Infrastructure;
public sealed class PostgresTicketStore(NpgsqlDataSource source) : ITicketStore
{
    public async Task InitializeLabAsync(CancellationToken ct)
    {
        string? schema = new NpgsqlConnectionStringBuilder(source.ConnectionString).SearchPath;
        if (schema is null || !Regex.IsMatch(schema, "^testing74_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Use a fresh isolated testing74_<32 hex> schema.");
        await using var stream = typeof(PostgresTicketStore).Assembly.GetManifestResourceStream("SupportDesk.Persistence.schema.sql")
            ?? throw new InvalidOperationException("Missing embedded schema.");
        using var text = new StreamReader(stream);
        string ddl = await text.ReadToEndAsync(ct);
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        // SQL identifiers require an allowlist, not a value parameter. No destructive reset.
        await using var create = new NpgsqlCommand("CREATE SCHEMA " + schema, connection, transaction);
        await create.ExecuteNonQueryAsync(ct);
        await using var tables = new NpgsqlCommand(ddl, connection, transaction);
        await tables.ExecuteNonQueryAsync(ct);
        await transaction.CommitAsync(ct);
    }
    public async Task<TicketSnapshot> CreateAsync(TicketTitle title, string owner, CancellationToken ct)
    {
        var ticket = Ticket.Open(Guid.NewGuid(), title);
        await using var command = source.CreateCommand("INSERT INTO tickets(id,owner_id,title,status,version) VALUES ($1,$2,$3,'Open',1)");
        command.Parameters.AddWithValue(ticket.Id); command.Parameters.AddWithValue(owner); command.Parameters.AddWithValue(title.Value);
        await command.ExecuteNonQueryAsync(ct); return new(ticket, 1, owner);
    }
    public async Task<TicketSnapshot?> LoadAsync(Guid id, CancellationToken ct)
    {
        await using var command = source.CreateCommand("SELECT id,owner_id,title,status,resolution,version FROM tickets WHERE id=$1");
        command.Parameters.AddWithValue(id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var ticket = Ticket.Open(reader.GetGuid(0), TicketTitle.Create(reader.GetString(2)));
        string status = reader.GetString(3);
        if (status is "InProgress" or "Closed") ticket = ticket.Start();
        if (status == "Closed") ticket = ticket.Resolve(reader.GetString(4));
        return new(ticket, reader.GetInt64(5), reader.GetString(1));
    }
    public async Task<SaveOutcome> SaveAsync(TicketSnapshot expected, Ticket changed, CancellationToken ct)
    {
        if (expected.Value.Id != changed.Id) throw new ArgumentException("Identity cannot change.", nameof(changed));
        await using var command = source.CreateCommand("UPDATE tickets SET title=$1,status=$2,resolution=$3,version=version+1 WHERE id=$4 AND owner_id=$5 AND version=$6");
        command.Parameters.AddWithValue(changed.Title.Value); command.Parameters.AddWithValue(changed.Status.ToString());
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text, Value = (object?)changed.Resolution ?? DBNull.Value });
        command.Parameters.AddWithValue(changed.Id); command.Parameters.AddWithValue(expected.OwnerId); command.Parameters.AddWithValue(expected.Version);
        if (await command.ExecuteNonQueryAsync(ct) == 1) return SaveOutcome.Saved;
        var current = await LoadAsync(changed.Id, ct);
        return current is null || current.OwnerId != expected.OwnerId ? SaveOutcome.Missing : SaveOutcome.Conflict;
    }
}
