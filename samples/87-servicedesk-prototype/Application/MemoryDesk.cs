using SupportDesk.Domain;
namespace SupportDesk.Application;
public sealed class MemoryDesk
{
    private readonly object gate=new();
    private readonly Dictionary<Guid,DeskRequest> rows=new();
    public (int Status,RequestView? View) Create(Actor actor,Guid key,string? title,string? priority)
    {
        lock(gate)
        {
            var previous=rows.Values.SingleOrDefault(r=>r.Tenant==actor.Tenant && r.Owner==actor.Id && r.Key==key);
            if(previous is not null) return previous.Title==title?.Trim() && previous.Priority==priority ? (200,RequestView.From(previous)) : (409,null);
            var request=DeskRequest.Open(actor,key,title,priority,DateTimeOffset.UtcNow); rows.Add(request.Id,request);return(201,RequestView.From(request));
        }
    }
    public RequestView? Read(Actor actor,Guid id) { lock(gate) return rows.TryGetValue(id,out var row)&&row.Visible(actor)?RequestView.From(row):null; }
    public (int Status,RequestView? View) Claim(Actor actor,Guid id,int version)
    {
        lock(gate)
        {
            if(!rows.TryGetValue(id,out var row)||!row.Visible(actor))return(404,null);
            if(!actor.Agent)return(403,null);
            return row.Claim(actor,version)?(200,RequestView.From(row)):(409,null);
        }
    }
}
