using System.ComponentModel.DataAnnotations;
namespace SupportDesk.Contracts;
public sealed class CreateTicketRequest
{
    [Required, StringLength(120, MinimumLength = 3)]
    public string? Title { get; init; }
}
