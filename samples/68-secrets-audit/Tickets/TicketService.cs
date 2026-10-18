using System.Security.Claims;
using SupportDesk.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Authorization;
using SupportDesk.Persistence;
namespace SupportDesk.Tickets;
public sealed class TicketService(AccountContext db, AccountAccess accounts, IAuthorizationService authorization, SecurityEvents security)
{
    public async Task<IResult> ListAsync(ClaimsPrincipal user, string? term, CancellationToken ct)
    {
        var actor = await accounts.ReadAsync(user);
        if (actor is null) return Results.Forbid();
        if (term is { Length: > 120 }) return Results.BadRequest();
        // Scope before projection, search and limit. Values become SQL parameters, not SQL syntax.
        var query = db.Tickets.AsNoTracking().Where(t => t.OwnerId == actor.UserId && t.TenantId == actor.TenantId);
        if (!string.IsNullOrEmpty(term)) query = query.Where(t => t.Title.Contains(term));
        return Results.Ok(await query.OrderBy(t => t.Id).Take(50).Select(t => new TicketView(t.Id, t.Title)).ToArrayAsync(ct));
    }
    public async Task<IResult> CreateAsync(ClaimsPrincipal user, TicketInput input, CancellationToken ct)
    {
        var actor = await accounts.ReadAsync(user);
        if (actor is null) return Results.Forbid();
        string? title = ValidTitle(input.Title);
        if (title is null) return Results.BadRequest();
        var ticket = new OwnedTicket { Id = Guid.NewGuid(), Title = title, OwnerId = actor.UserId, TenantId = actor.TenantId };
        db.Tickets.Add(ticket);
        db.AuditEntries.Add(AuditEntry.For(AuditKind.TicketCreated, actor.UserId, actor.TenantId, ticket.Id));
        // One SaveChanges transaction covers both inserts; no success event precedes it.
        await db.SaveChangesAsync(ct);
        security.Record(SecurityCode.TicketCreated, actor.UserId, ticket.Id);
        return Results.Created("/api/tickets/" + ticket.Id, new TicketView(ticket.Id, ticket.Title));
    }
    public async Task<IResult> ReadAsync(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var ticket = await db.Tickets.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null || !(await authorization.AuthorizeAsync(user, ticket, TicketOperations.Read)).Succeeded)
        { security.Record(SecurityCode.AccessDenied); return Results.NotFound(); }
        return Results.Ok(new TicketView(ticket.Id, ticket.Title));
    }
    public async Task<IResult> RenameAsync(ClaimsPrincipal user, Guid id, TicketInput input, CancellationToken ct)
    {
        var ticket = await db.Tickets.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, ct);
        if (ticket is null || !(await authorization.AuthorizeAsync(user, ticket, TicketOperations.Rename)).Succeeded)
        { security.Record(SecurityCode.AccessDenied); return Results.NotFound(); }
        string? title = ValidTitle(input.Title);
        if (title is null) return Results.BadRequest();
        // Repeat ownership in the write predicate; never attach and update a client-supplied entity.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        int changed = await db.Tickets.Where(t => t.Id == id && t.OwnerId == ticket.OwnerId && t.TenantId == ticket.TenantId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Title, title), ct);
        if (changed != 1) return Results.NotFound();
        db.AuditEntries.Add(AuditEntry.For(AuditKind.TicketRenamed, ticket.OwnerId, ticket.TenantId, ticket.Id));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        security.Record(SecurityCode.TicketRenamed, ticket.OwnerId, ticket.Id);
        return Results.NoContent();
    }
    private static string? ValidTitle(string? value) => value?.Trim() is { Length: >= 3 and <= 120 } title ? title : null;
}
