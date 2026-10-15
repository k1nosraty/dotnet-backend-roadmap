using System.ComponentModel.DataAnnotations;
namespace SupportDesk.Contracts;
public sealed class AddNoteRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public string? Text { get; init; }
}
