using System.ComponentModel.DataAnnotations;
namespace SupportDesk.Contracts;
public sealed record CreateTicketRequest(
    [property: Required, StringLength(120, MinimumLength = 3)] string? Title);
