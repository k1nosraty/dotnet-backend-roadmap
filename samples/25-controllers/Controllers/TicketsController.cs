using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application;
using SupportDesk.Contracts;
namespace SupportDesk.Controllers;
[ApiController]
[Route("api/tickets")]
public sealed class TicketsController(TicketService service ) : ControllerBase
{
    [HttpGet]
    public ActionResult<TicketResponse[]> All() => Ok(service.All().Select(t => new TicketResponse(t.Id, t.Title)).ToArray());
    [HttpGet("{id:int}")]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<TicketResponse> GetById(int id)
    {
        if (id <= 0) return Problem(statusCode: 400, title: "ID must be positive");
        var ticket = service.Find(id);
        if (ticket is null) return Problem(statusCode: 404, title: "Ticket not found");
        return Ok(new TicketResponse(ticket.Id, ticket.Title));
    }
    [HttpPost]
    [ProducesResponseType<TicketResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<TicketResponse> Create([FromBody] CreateTicketRequest request)
    {
        string? title = request.Title?.Trim();
        if (title is null || title.Length < 3 || title.Length > 120) return InvalidTitle();
        var ticket = service.Create(title);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, new TicketResponse(ticket.Id, ticket.Title));
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
}
