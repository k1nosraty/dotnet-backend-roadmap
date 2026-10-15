using System.ComponentModel.DataAnnotations;
namespace SupportDesk.Contracts;
public sealed class AddNoteRequest
{
    [Required, StringLength(500, MinimumLength = 1)]
    public string? Text { get; init; }
}
