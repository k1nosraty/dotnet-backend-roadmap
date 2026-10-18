using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.DataProtection;
using SupportDesk.Accounts;
using SupportDesk.Infrastructure;
using SupportDesk.Persistence;

return await Lab.RunAsync("auth62", args, async (source, ct) =>
{
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();
    AccountServices.AddAccounts(builder.Services, o => o.UseNpgsql(source));
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
    await using var app = builder.Build();
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AccountContext>();
    // Only the fresh schema owned by Lab is touched. No application database is dropped.
    await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(ct);
    await AccountChecks.RunAsync(scope.ServiceProvider);

});
