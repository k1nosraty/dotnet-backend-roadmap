namespace SupportDesk.Hosting;
public sealed class StartupStamp
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}
