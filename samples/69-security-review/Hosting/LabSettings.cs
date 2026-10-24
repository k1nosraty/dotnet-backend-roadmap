using System.Text.RegularExpressions;
using Npgsql;
namespace SupportDesk.Hosting;
// Do not serialize configuration or use a record with a generated secret-bearing ToString.
public sealed class LabSettings
{
    public string Connection { get; }
    public string Keys { get; }
    private LabSettings(string connection, string keys) { Connection = connection; Keys = keys; }
    public static LabSettings Read()
    {
        string? connection = Environment.GetEnvironmentVariable("SUPPORTDESK_PG");
        string? keys = Environment.GetEnvironmentVariable("TEAMDESK_KEYS");
        if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(keys) || !Path.IsPathFullyQualified(keys))
            throw new InvalidOperationException("Set SUPPORTDESK_PG and an absolute private TEAMDESK_KEYS path.");
        try
        {
            var parsed = new NpgsqlConnectionStringBuilder(connection);
            if (string.IsNullOrWhiteSpace(parsed.Host) || string.IsNullOrWhiteSpace(parsed.Database) ||
                string.IsNullOrWhiteSpace(parsed.Username) || parsed.IncludeErrorDetail || parsed.LogParameters ||
                parsed.SearchPath is not { } schema || !Regex.IsMatch(schema, "^auth70_[a-f0-9]{32}$"))
                throw new ArgumentException();
            return new(parsed.ConnectionString, keys);
        }
        catch (ArgumentException)
        {
            // No raw value or inner exception in startup diagnostics.
            throw new InvalidOperationException("Invalid isolated lab database configuration.");
        }
    }
}
