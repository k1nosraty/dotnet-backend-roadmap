using SupportDesk.Application;
namespace SupportDesk.Features;

internal static class OutcomeHttp
{
    public static IResult ToHttp(OperationResult result) => result.Code switch
    {
        OperationCode.Success => Results.Ok(result.Ticket),
        OperationCode.Invalid => Results.Problem(statusCode: 400, title: "Invalid request."),
        OperationCode.Missing => Results.Problem(statusCode: 404, title: "Ticket not found."),
        OperationCode.Conflict => Results.Problem(statusCode: 409, title: "Ticket change conflicts."),
        _ => throw new InvalidOperationException("Unknown application outcome.")
    };
}
