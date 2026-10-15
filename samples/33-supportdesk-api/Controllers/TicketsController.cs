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
    public ActionResult<PageResponse<TicketResponse>> All([FromQuery] string? q,
        [FromQuery] int page = 1, [FromQuery] int? pageSize = null)
    {
        int size = pageSize ?? options.Value.DefaultPageSize;
        if (page < 1 || size < 1 || size > options.Value.MaxPageSize || (q?.Length ?? 0) > 120)
            return Problem(statusCode: 400, title: "Invalid page or filter");
        // One immutable snapshot: count and page are consistent within this request.
        var filtered = service.All().Where(t => string.IsNullOrWhiteSpace(q)
            || t.Title.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
        long skip = ((long)page - 1) * size;
        TicketResponse[] items = skip >= filtered.Length ? [] : filtered.Skip((int)skip).Take(size).Select(t => t.ToResponse()).ToArray();
        return Ok(new PageResponse<TicketResponse>(items, page, size, filtered.Length));
    }
    [HttpGet("{id:int}")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<TicketResponse> GetById(int id)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "ID must be positive");
        var ticket = service.Find(id);
        if (ticket is null) return Problem(statusCode: 404, title: "Ticket not found");
        return Ok(ticket.ToResponse());
    }
    [HttpPost]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<TicketResponse> Create([FromBody] CreateTicketRequest request)
    {
        string? title = request.Title?.Trim();
        if (title is null || title.Length < 3 || title.Length > 120) return InvalidTitle();
        var ticket = service.Create(title);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket.ToResponse());
    }
    [HttpPut("{id:int}/title")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<TicketResponse> ReplaceTitle(int id, [FromBody] ReplaceTitleRequest request)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "ID must be positive");
        string? title = request.Title?.Trim();
        if (title is null || title.Length < 3 || title.Length > 120) return InvalidTitle();
        var ticket = service.Rename(id, title);
        if (ticket is null) return Problem(statusCode: 404, title: "Ticket not found");
        return Ok(ticket.ToResponse());
    }
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult Delete(int id)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "ID must be positive");
        if (!service.Delete(id)) return Problem(statusCode: 404, title: "Ticket not found");
        return NoContent();
    }
    private ActionResult InvalidTitle()
    {
        ModelState.AddModelError("title", "Title must contain 3-120 characters after trimming.");
        return ValidationProblem(ModelState);
    }
    [HttpPost("{id:int}/notes")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<TicketResponse> AddNote(int id, [FromBody] AddNoteRequest request)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "ID must be positive");
        string? text = request.Text?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > 500)
        {
            ModelState.AddModelError("text", "Note must contain 1-500 characters after trimming.");
            return ValidationProblem(ModelState);
        }
        return FromMutation(service.AddNote(id, text));
    }
    [HttpPut("{id:int}/status")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<TicketResponse> ChangeStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "ID must be positive");
        if (request.Status is not ("Open" or "InProgress" or "Closed"))
            return Problem(statusCode: 400, title: "Unknown status");
        return FromMutation(service.ChangeStatus(id, request.Status));
    }
    private ActionResult<TicketResponse> FromMutation(MutationResult result) => result.Kind switch
    {
        MutationKind.Missing => Problem(statusCode: 404, title: "Ticket not found"),
        MutationKind.Conflict => Problem(statusCode: 409, title: "Transition is not allowed; closing requires a note"),
        _ => Ok(result.Value!.ToResponse())
    };
}
