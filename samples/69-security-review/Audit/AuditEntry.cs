namespace SupportDesk.Audit;
public enum AuditKind { TicketCreated, TicketRenamed }
public sealed class AuditEntry
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string ActorId { get; set; } = "";
    public string TenantId { get; set; } = "";
    public AuditKind Kind { get; set; }
    public Guid TargetId { get; set; }
    public static AuditEntry For(AuditKind kind, string actor, string tenant, Guid target) => new()
    { Id = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow, ActorId = actor, TenantId = tenant, Kind = kind, TargetId = target };
}
public enum SecurityCode { LoginSucceeded, LoginRejected, AccessDenied, TicketCreated, TicketRenamed, ServerFailure }
public sealed class SecurityEvents(ILogger<SecurityEvents> logger)
{
    // Only fixed event names and opaque server-owned identifiers enter the structured event.
    // Never accept an arbitrary message, email, title, token, header, query or exception here.
    public void Record(SecurityCode code, string? actor = null, Guid? target = null) =>
        logger.LogInformation(new EventId(6900 + (int)code, code.ToString()),
            "SecurityEvent {Code} Actor {ActorId} Target {TargetId}", code, actor, target);
}
