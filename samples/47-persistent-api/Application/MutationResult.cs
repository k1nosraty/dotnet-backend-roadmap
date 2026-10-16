using SupportDesk.Domain;
namespace SupportDesk.Application;

public enum MutationKind { Success, Missing, Conflict }

public sealed class MutationResult
{
    private readonly Ticket? value;
    public MutationKind Kind { get; }
    public Ticket Value => value ?? throw new InvalidOperationException("Failure has no ticket.");

    private MutationResult(MutationKind kind, Ticket? value)
    {
        Kind = kind;
        this.value = value;
    }
    public static MutationResult Success(Ticket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return new(MutationKind.Success, ticket);
    }
    public static MutationResult Missing() => new(MutationKind.Missing, null);
    public static MutationResult Conflict() => new(MutationKind.Conflict, null);
}
