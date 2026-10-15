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
    public ActionResult<TicketResponse[]> All() => Ok(service.All().Select(t => t.ToResponse()).ToArray());
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
    [HttpGet("settings")]
    public IActionResult Settings() => Ok(new { options.Value.DefaultPageSize, options.Value.MaxPageSize });
}
