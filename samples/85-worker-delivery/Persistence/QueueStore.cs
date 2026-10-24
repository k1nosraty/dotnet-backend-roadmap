using Npgsql;
namespace SupportDesk;
public sealed record Lease(Guid Id, Guid Token, int Attempt);
public sealed record QueueCounts(long Pending,long Leased,long Done,long Dead,long Effects);
public sealed class QueueStore(NpgsqlDataSource data)
{
    public const string Schema = """
        CREATE TABLE IF NOT EXISTS jobs (
            id uuid PRIMARY KEY, state text NOT NULL CHECK (state IN ('pending','leased','done','dead')),
            attempts integer NOT NULL DEFAULT 0 CHECK(attempts BETWEEN 0 AND 3),
            available_at timestamptz NOT NULL DEFAULT clock_timestamp(),
            lease_until timestamptz, lease_token uuid);
        CREATE INDEX IF NOT EXISTS jobs_due ON jobs(available_at,id) WHERE state IN ('pending','leased');
        CREATE TABLE IF NOT EXISTS effects(job_id uuid PRIMARY KEY REFERENCES jobs(id), value text NOT NULL);
        """;
    public async Task InitializeAsync(CancellationToken ct=default)
    { await using var command=data.CreateCommand(Schema); await command.ExecuteNonQueryAsync(ct); }
    public async Task EnqueueAsync(Guid id,CancellationToken ct=default)
    {
        if(id==Guid.Empty) throw new ArgumentException("Job ID is required.",nameof(id));
        await using var command=data.CreateCommand("INSERT INTO jobs(id,state) VALUES(@id,'pending') ON CONFLICT(id) DO NOTHING");
        command.Parameters.AddWithValue("id",id); await command.ExecuteNonQueryAsync(ct);
    }
    public async Task<Lease?> ClaimAsync(CancellationToken ct=default)
    {
        await using var connection=await data.OpenConnectionAsync(ct);
        await using var transaction=await connection.BeginTransactionAsync(ct);
        await using(var exhaust=new NpgsqlCommand("UPDATE jobs SET state='dead',lease_token=NULL,lease_until=NULL WHERE state='leased' AND lease_until <= clock_timestamp() AND attempts=3",connection,transaction))
            await exhaust.ExecuteNonQueryAsync(ct);
        await using var command=new NpgsqlCommand("""
            WITH candidate AS (
                SELECT id FROM jobs WHERE attempts<3 AND
                ((state='pending' AND available_at<=clock_timestamp()) OR (state='leased' AND lease_until<=clock_timestamp()))
                ORDER BY available_at,id LIMIT 1 FOR UPDATE SKIP LOCKED)
            UPDATE jobs j SET state='leased',attempts=j.attempts+1,lease_token=@token,
                lease_until=clock_timestamp()+interval '30 seconds'
            FROM candidate c WHERE j.id=c.id RETURNING j.id,j.attempts
            """,connection,transaction);
        var token=Guid.NewGuid(); command.Parameters.AddWithValue("token",token);
        Lease? lease=null;
        await using(var reader=await command.ExecuteReaderAsync(ct))
            if(await reader.ReadAsync(ct)) lease=new(reader.GetGuid(0),token,reader.GetInt32(1));
        await transaction.CommitAsync(ct); return lease;
    }
    public async Task<bool> CompleteAsync(Lease lease,CancellationToken ct=default)
    {
        await using var connection=await data.OpenConnectionAsync(ct);
        await using var transaction=await connection.BeginTransactionAsync(ct);
        await using(var owns=new NpgsqlCommand("SELECT id FROM jobs WHERE id=@id AND state='leased' AND lease_token=@token AND lease_until>clock_timestamp() FOR UPDATE",connection,transaction))
        {
            owns.Parameters.AddWithValue("id",lease.Id); owns.Parameters.AddWithValue("token",lease.Token);
            if(await owns.ExecuteScalarAsync(ct) is null) return false;
        }
        // Only this local database effect is atomic with acknowledgment; external HTTP/email is not.
        await using(var effect=new NpgsqlCommand("INSERT INTO effects(job_id,value) VALUES(@id,'processed') ON CONFLICT(job_id) DO NOTHING",connection,transaction))
        { effect.Parameters.AddWithValue("id",lease.Id); await effect.ExecuteNonQueryAsync(ct); }
        await using(var ack=new NpgsqlCommand("UPDATE jobs SET state='done',lease_until=NULL,lease_token=NULL WHERE id=@id",connection,transaction))
        { ack.Parameters.AddWithValue("id",lease.Id); await ack.ExecuteNonQueryAsync(ct); }
        await transaction.CommitAsync(ct); return true;
    }
    public async Task<bool> FailAsync(Lease lease,CancellationToken ct=default)
    {
        await using var command=data.CreateCommand("""
            UPDATE jobs SET state=CASE WHEN attempts>=3 THEN 'dead' ELSE 'pending' END,
                available_at=clock_timestamp()+interval '2 seconds',lease_until=NULL,lease_token=NULL
            WHERE id=@id AND state='leased' AND lease_token=@token AND lease_until>clock_timestamp()
            """);
        command.Parameters.AddWithValue("id",lease.Id); command.Parameters.AddWithValue("token",lease.Token);
        return await command.ExecuteNonQueryAsync(ct)==1;
    }
    public async Task<QueueCounts> CountsAsync(CancellationToken ct=default)
    {
        await using var command=data.CreateCommand("""
            SELECT count(*) FILTER(WHERE state='pending'),count(*) FILTER(WHERE state='leased'),
                count(*) FILTER(WHERE state='done'),count(*) FILTER(WHERE state='dead'),(SELECT count(*) FROM effects) FROM jobs
            """);
        await using var reader=await command.ExecuteReaderAsync(ct); await reader.ReadAsync(ct);
        return new(reader.GetInt64(0),reader.GetInt64(1),reader.GetInt64(2),reader.GetInt64(3),reader.GetInt64(4));
    }
}
