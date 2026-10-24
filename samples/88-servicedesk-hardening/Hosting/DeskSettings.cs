using System.Text.RegularExpressions;
using Npgsql;
namespace SupportDesk.Hosting;
public sealed class DeskSettings
{
    public string Connection {get;set;}="";
    public string Keys {get;set;}="";
    public bool LabHttp {get;set;}
    public static string Schema(string connection)
    {
        var schema=new NpgsqlConnectionStringBuilder(connection).SearchPath;
        if(schema is null||!Regex.IsMatch(schema,"^capstone_[a-f0-9]{32}$"))throw new InvalidOperationException("Use an isolated capstone_<32 hex> schema.");
        return schema;
    }
    public static void Load(DeskSettings settings,IConfiguration config,IHostEnvironment environment)
    {
        settings.Connection=config["Desk:Connection"]??"";settings.Keys=config["Desk:Keys"]??"";
        settings.LabHttp=config.GetValue("Desk:LabHttp",false);
        try { Schema(settings.Connection); }
        catch(Exception ex) when(ex is ArgumentException or InvalidOperationException)
        {throw new InvalidOperationException("Invalid Desk:Connection lab configuration.");}
        if(!Path.IsPathFullyQualified(settings.Keys))throw new InvalidOperationException("Desk:Keys must be an absolute private directory.");
        if(settings.LabHttp&&!environment.IsDevelopment())throw new InvalidOperationException("LabHttp requires Development.");
    }
}
