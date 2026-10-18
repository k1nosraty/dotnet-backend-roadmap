using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SupportDesk.Persistence;
namespace SupportDesk.Hosting;

public static class LabProvisioning
{
    public static async Task InitializeAsync(IServiceProvider services, string connection)
    {
        string? schema = new NpgsqlConnectionStringBuilder(connection).SearchPath;
        if (schema is null || !Regex.IsMatch(schema, "^auth64_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Use a fresh auth64_<32 lowercase hex> lab schema.");
        string email = Environment.GetEnvironmentVariable("TEAMDESK_LAB_EMAIL") ?? throw new InvalidOperationException("Set TEAMDESK_LAB_EMAIL.");
        string password = Environment.GetEnvironmentVariable("TEAMDESK_LAB_PASSWORD") ?? throw new InvalidOperationException("Set TEAMDESK_LAB_PASSWORD.");
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountContext>();
        // SQL identifiers cannot be value parameters. The strict generated-name allowlist above is mandatory.
        await using (var control = NpgsqlDataSource.Create(connection))
        {
            await using var command = control.CreateCommand("CREATE SCHEMA IF NOT EXISTS " + schema);
            await command.ExecuteNonQueryAsync();
        }
        // The caller owns a fresh lab schema. This fails rather than replacing existing tables.
        await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = email, Email = email };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) throw new InvalidOperationException("Lab account did not satisfy account policy.");
        var code = await users.GenerateEmailConfirmationTokenAsync(user);
        if (!(await users.ConfirmEmailAsync(user, code)).Succeeded) throw new InvalidOperationException("Lab confirmation failed.");
        Console.WriteLine("PASS: fresh authentication lab initialized; no credentials printed");
    }
}
