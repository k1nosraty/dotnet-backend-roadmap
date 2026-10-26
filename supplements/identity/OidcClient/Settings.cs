namespace IdentityLab;
public static class Settings
{
    public static string Required(IConfiguration config,string key)
    {
        var value=config[key];
        if(string.IsNullOrWhiteSpace(value)||value.Length>2048)throw new InvalidOperationException("Required identity configuration is missing or invalid.");
        return value;
    }
    public static string Authority(IConfiguration config)
    {
        var value=Required(config,"Tokens:Authority").TrimEnd('/');
        if(!Uri.TryCreate(value,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.UserInfo!=""||uri.Query!=""||uri.Fragment!="")
            throw new InvalidOperationException("Authority must be an absolute HTTPS issuer URL.");
        return value;
    }
}
