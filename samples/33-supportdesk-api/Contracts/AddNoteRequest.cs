using System.ComponentModel.DataAnnotations;
namespace SupportDesk.Contracts;
public sealed record AddNoteRequest([property: Required, StringLength(500, MinimumLength = 1)] string? Text);
