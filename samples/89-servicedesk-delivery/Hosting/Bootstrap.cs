using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using SupportDesk.Persistence;
namespace SupportDesk.Hosting;
public static class Bootstrap
{
    public static async Task InitializeAsync(IServiceProvider services,string password)
    {
        await using var scope=services.CreateAsyncScope();
        var settings=scope.ServiceProvider.GetRequiredService<IOptions<DeskSettings>>().Value;
        var schema=DeskSettings.Schema(settings.Connection);
        await using(var source=NpgsqlDataSource.Create(settings.Connection))
        {await using var command=source.CreateCommand("CREATE SCHEMA "+schema);await command.ExecuteNonQueryAsync();}
        var db=scope.ServiceProvider.GetRequiredService<DeskContext>();
        await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
        var users=scope.ServiceProvider.GetRequiredService<UserManager<DeskUser>>();
        foreach(var (email,tenant,agent) in new[] {
            ("requester@alpha.test","11111111-1111-1111-1111-111111111111",false),
            ("peer@alpha.test","11111111-1111-1111-1111-111111111111",false),
            ("agent@alpha.test","11111111-1111-1111-1111-111111111111",true),
            ("requester@beta.test","22222222-2222-2222-2222-222222222222",false) })
        {
            var user=new DeskUser {UserName=email,Email=email,EmailConfirmed=true,Tenant=Guid.Parse(tenant),IsAgent=agent};
            if(!(await users.CreateAsync(user,password)).Succeeded)throw new InvalidOperationException("Lab account provisioning failed.");
        }
        Console.WriteLine("schema=ready; accounts=4; credentials=not-printed");
    }
}
