using Microsoft.EntityFrameworkCore;
using Npgsql;
using SupportDesk.Domain;
using SupportDesk.Persistence;
namespace SupportDesk.Application;
public sealed record DeskResult(int Status,RequestView? View=null);
public sealed record AuditView(string Kind,DateTimeOffset At,string ActorId);
public sealed class DeskService(DeskContext db,TimeProvider clock)
{
    private IQueryable<DeskRequest> Visible(Actor actor)=>db.Requests.Where(r=>r.Tenant==actor.Tenant&&(actor.Agent||r.Owner==actor.Id));
    public Task<DeskRequest?> ReadAsync(Actor actor,Guid id,CancellationToken ct)=>Visible(actor).AsNoTracking().SingleOrDefaultAsync(r=>r.Id==id,ct);
    public async Task<RequestView[]> ListAsync(Actor actor,int page,int size,CancellationToken ct)
    {
        var rows=await Visible(actor).AsNoTracking().OrderBy(r=>r.CreatedAt).ThenBy(r=>r.Id).Skip((page-1)*size).Take(size).ToArrayAsync(ct);
        return rows.Select(RequestView.From).ToArray();
    }
    public async Task<DeskResult> CreateAsync(Actor actor,Guid key,string? title,string? priority,CancellationToken ct)
    {
        if(key==Guid.Empty||!DeskRequest.ValidText(title,120)||priority is not("normal" or "urgent"))return new(400);
        var existing=await db.Requests.AsNoTracking().SingleOrDefaultAsync(r=>r.Tenant==actor.Tenant&&r.Owner==actor.Id&&r.Key==key,ct);
        if(existing is not null)return Replay(existing,title!,priority);
        var request=DeskRequest.Open(actor,key,title,priority,clock.GetUtcNow());
        try
        {
            await using var transaction=await db.Database.BeginTransactionAsync(ct);
            db.Requests.Add(request);await db.SaveChangesAsync(ct);
            AddAudit(actor,request,"Created");await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);return new(201,RequestView.From(request));
        }
        catch(DbUpdateException ex) when(ex.InnerException is PostgresException {SqlState:"23505",ConstraintName:"ux_request_key"})
        {
            db.ChangeTracker.Clear();
            existing=await db.Requests.AsNoTracking().SingleAsync(r=>r.Tenant==actor.Tenant&&r.Owner==actor.Id&&r.Key==key,ct);
            return Replay(existing,title!,priority);
        }
    }
    private static DeskResult Replay(DeskRequest row,string title,string priority)=>row.Title==title.Trim()&&row.Priority==priority?new(200,RequestView.From(row)):new(409);
    public async Task<DeskResult> TransitionAsync(Actor actor,Guid id,int version,string action,string? resolution,CancellationToken ct)
    {
        await using var transaction=await db.Database.BeginTransactionAsync(ct);
        var request=await Visible(actor).SingleOrDefaultAsync(r=>r.Id==id,ct);if(request is null)return new(404);
        if((action is "claim" or "close")&&!actor.Agent || action=="reopen"&&request.Owner!=actor.Id)return new(403);
        if(action=="close"&&!DeskRequest.ValidText(resolution,500))return new(400);
        var changed=action switch {
            "claim"=>request.Claim(actor,version),"close"=>request.Close(actor,version,resolution),
            "reopen"=>request.Reopen(actor,version,clock.GetUtcNow()),_=>false};
        if(!changed)return new(409);
        try
        {
            await db.SaveChangesAsync(ct);
            AddAudit(actor,request,action switch {"claim"=>"Claimed","close"=>"Closed",_=>"Reopened"});
            await db.SaveChangesAsync(ct);await transaction.CommitAsync(ct);return new(200,RequestView.From(request));
        }
        catch(DbUpdateConcurrencyException){return new(409);}
    }
    public async Task<AuditView[]?> AuditAsync(Actor actor,Guid id,CancellationToken ct)
    {
        if(!await Visible(actor).AnyAsync(r=>r.Id==id,ct))return null;
        return await db.Audits.AsNoTracking().Where(a=>a.Tenant==actor.Tenant&&a.RequestId==id)
            .OrderBy(a=>a.At).ThenBy(a=>a.Id).Take(100).Select(a=>new AuditView(a.Kind,a.At,a.ActorId)).ToArrayAsync(ct);
    }
    private void AddAudit(Actor actor,DeskRequest request,string kind)=>db.Audits.Add(new DeskAudit {
        RequestId=request.Id,Tenant=actor.Tenant,ActorId=actor.Id,Kind=kind,At=clock.GetUtcNow()});
}
