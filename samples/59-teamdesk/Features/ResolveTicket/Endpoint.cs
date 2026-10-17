using SupportDesk.Application;
using SupportDesk.Presentation;
namespace SupportDesk.Features;

internal static class ResolveEndpoint
{
    public static void MapResolveTicket(this WebApplication app)
    {
        app.MapPost("/api/tickets/{id:int}/resolve", async (int id, ResolveRequest request,
            ResolveTicket handler, ResolveTicketValidator validator, CancellationToken ct) =>
        {
            var command = RequestMapping.ToCommand(id, request);
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.Errors.GroupBy(x => x.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()));
            return OutcomeHttp.ToHttp(await handler.ExecuteAsync(command, ct));
        });
    }
}
