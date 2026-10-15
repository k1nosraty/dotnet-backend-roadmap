using SupportDesk.Domain;
namespace SupportDesk.Application;
public enum MutationKind { Success, Missing, Conflict }
public sealed record MutationResult(MutationKind Kind, Ticket? Value = null);
