using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
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
        if (schema is null || !Regex.IsMatch(schema, "^auth70_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Use a fresh auth70_<32 lowercase hex> lab schema.");
        string password = Environment.GetEnvironmentVariable("TEAMDESK_LAB_PASSWORD") ?? throw new InvalidOperationException("Set TEAMDESK_LAB_PASSWORD.");
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountContext>();
        await using (var control = NpgsqlDataSource.Create(connection))
        {
            // Identifier is generated and strictly allowlisted above, never an HTTP input.
            await using var command = control.CreateCommand("CREATE SCHEMA IF NOT EXISTS " + schema);
            await command.ExecuteNonQueryAsync();
        }
        await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Ensure(await roles.CreateAsync(new IdentityRole("SupportAgent")));
        foreach (var (email, tenant, role, permission) in new[] {
            ("owner@example.test", "alpha", false, false),
            ("peer@example.test", "alpha", false, false),
            ("other@example.test", "beta", false, false),
            ("staff@example.test", "alpha", true, true),
            ("roleonly@example.test", "alpha", true, false),
            ("claimonly@example.test", "alpha", false, true) })
        {
            var user = new IdentityUser { UserName = email, Email = email };
            Ensure(await users.CreateAsync(user, password));
            Ensure(await users.ConfirmEmailAsync(user, await users.GenerateEmailConfirmationTokenAsync(user)));
            Ensure(await users.AddClaimAsync(user, new Claim("tenant", tenant)));
            if (role) Ensure(await users.AddToRoleAsync(user, "SupportAgent"));
            if (permission) Ensure(await users.AddClaimAsync(user, new Claim("permission", "tickets.manage")));
            if (email == "staff@example.test") Ensure(await users.AddClaimAsync(user, new Claim("permission", "audit.read")));
        }
        Console.WriteLine("PASS: fresh access lab initialized; no credentials printed");
    }
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("Lab provisioning failed account policy or storage validation.");
    }
}
