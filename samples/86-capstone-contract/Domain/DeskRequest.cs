using System.Text;
namespace SupportDesk.Domain;
public sealed record Actor(string Id,Guid Tenant,bool Agent);
public enum DeskState { Open, InProgress, Closed }
public sealed class DeskRequest
{
    private DeskRequest() { }
    public Guid Id { get; private set; }
    public Guid Tenant { get; private set; }
    public string Owner { get; private set; }="";
    public Guid Key { get; private set; }
    public string Title { get; private set; }="";
    public string Priority { get; private set; }="normal";
    public DeskState State { get; private set; }
    public int Version { get; private set; }
    public int Reopens { get; private set; }
    public string? AssignedTo { get; private set; }
    public string? Resolution { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset DueAt { get; private set; }
    public static bool ValidText(string? value,int maximum)
    { var count=value?.Trim().EnumerateRunes().Count()??0; return count>=3 && count<=maximum; }
    public static DeskRequest Open(Actor actor,Guid key,string? title,string? priority,DateTimeOffset now)
    {
        if(actor.Tenant==Guid.Empty || string.IsNullOrWhiteSpace(actor.Id) || key==Guid.Empty || !ValidText(title,120) || priority is not ("normal" or "urgent"))
            throw new ArgumentException("Invalid request contract.");
        return new() { Id=Guid.NewGuid(),Tenant=actor.Tenant,Owner=actor.Id,Key=key,Title=title!.Trim(),Priority=priority,
            State=DeskState.Open,Version=1,CreatedAt=now,DueAt=now.AddHours(priority=="urgent"?4:24) };
    }
    public bool Visible(Actor actor)=>actor.Tenant==Tenant && (actor.Id==Owner || actor.Agent);
    public bool Claim(Actor actor,int expected)
    {
        if(!Visible(actor) || !actor.Agent || Version!=expected || State!=DeskState.Open) return false;
        AssignedTo=actor.Id; State=DeskState.InProgress; Version++; return true;
    }
    public bool Close(Actor actor,int expected,string? resolution)
    {
        if(!Visible(actor) || !actor.Agent || AssignedTo!=actor.Id || Version!=expected || State!=DeskState.InProgress || !ValidText(resolution,500)) return false;
        Resolution=resolution!.Trim(); State=DeskState.Closed; Version++; return true;
    }
    public bool Reopen(Actor actor,int expected,DateTimeOffset now)
    {
        if(actor.Tenant!=Tenant || actor.Id!=Owner || Version!=expected || State!=DeskState.Closed || Reopens!=0) return false;
        State=DeskState.Open; AssignedTo=null; Resolution=null; Reopens++; Version++; DueAt=now.AddHours(Priority=="urgent"?4:24); return true;
    }
}
public sealed record RequestView(Guid Id,string Title,string Priority,string State,int Version,int Reopens,string? AssignedTo,string? Resolution,DateTimeOffset DueAt)
{
    public static RequestView From(DeskRequest request)=>new(request.Id,request.Title,request.Priority,request.State.ToString(),request.Version,
        request.Reopens,request.AssignedTo,request.Resolution,request.DueAt);
}
