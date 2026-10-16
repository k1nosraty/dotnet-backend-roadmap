using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application;
using SupportDesk.Contracts;
using SupportDesk.Mapping;
using Microsoft.Extensions.Options;
using SupportDesk.Configuration;
namespace SupportDesk.Controllers;
[ApiController]
[Route("api/tickets")]
public sealed class TicketsController(TicketService service, IOptions<TicketOptions> options ) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PageResponse<TicketResponse>>> All([FromQuery] string? q,
        [FromQuery] int page = 1, [FromQuery] int? pageSize = null, CancellationToken ct = default)
    {
        int size = pageSize ?? options.Value.DefaultPageSize;
        if (page < 1 || size < 1 || size > options.Value.MaxPageSize || (q?.Length ?? 0) > 120)
            return Problem(statusCode: 400, title: "Invalid page or filter");
        // PostgreSQL filtering uses case-sensitive Contains; see the delivery lesson.
        return Ok(await service.AllAsync(q, page, size, ct));
    }
    [HttpGet("{id:int}")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketResponse>> GetById(int id, CancellationToken ct)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "ID must be positive");
        var ticket = await service.FindAsync(id, ct);
        if (ticket is null) return Problem(statusCode: 404, title: "Ticket not found");
        return Ok(ticket.ToResponse());
    }
    [HttpPost]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketResponse>> Create([FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        string? title = request.Title?.Trim();
        if (title is null || title.Length < 3 || title.Length > 120) return InvalidTitle();
        var ticket = await service.CreateAsync(title, ct);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket.ToResponse());
    }
    [HttpPut("{id:int}/title")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketResponse>> ReplaceTitle(int id, [FromBody] ReplaceTitleRequest request, CancellationToken ct)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "ID must be positive");
        string? title = request.Title?.Trim();
        if (title is null || title.Length < 3 || title.Length > 120) return InvalidTitle();
        return FromMutation(await service.RenameAsync(id, title, ct));
    }
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "ID must be positive");
        var result = await service.DeleteAsync(id, ct);
        return result.Kind switch
        {
            MutationKind.Success => NoContent(),
            MutationKind.Missing => Problem(statusCode: 404, title: "Ticket not found"),
            MutationKind.Conflict => Problem(statusCode: 409, title: "Concurrent change; reload ticket"),
            _ => throw new InvalidOperationException("Unknown mutation result.")
        };
    }
    private ActionResult InvalidTitle()
    {
        ModelState.AddModelError("title", "Title must contain 3-120 characters after trimming.");
        return ValidationProblem(ModelState);
    }
    [HttpPost("{id:int}/notes")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> AddNote(int id, [FromBody] AddNoteRequest request, CancellationToken ct)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "ID must be positive");
        string? text = request.Text?.Trim();
        if (text is null || text.Length < 3 || text.Length > 500)
        {
            ModelState.AddModelError("text", "Note must contain 3-500 characters after trimming.");
            return ValidationProblem(ModelState);
        }
        return FromMutation(await service.AddNoteAsync(id, text, ct));
    }
    [HttpPut("{id:int}/status")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketResponse>> ChangeStatus(int id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "ID must be positive");
        if (request.Status is not ("Open" or "InProgress" or "Closed"))
            return Problem(statusCode: 400, title: "Unknown status");
        return FromMutation(await service.ChangeStatusAsync(id, request.Status, ct));
    }
    private ActionResult<TicketResponse> FromMutation(MutationResult result) => result.Kind switch
    {
        MutationKind.Missing => Problem(statusCode: 404, title: "Ticket not found"),
        MutationKind.Conflict => Problem(statusCode: 409, title: "Change conflicts with current ticket rules"),
        MutationKind.Success => Ok(result.Value.ToResponse()),
        _ => throw new InvalidOperationException("Unknown mutation result.")
    };
}
