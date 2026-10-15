using System.ComponentModel.DataAnnotations;
namespace SupportDesk.Contracts;
public sealed record ReplaceTitleRequest(
    [property: Required, StringLength(120, MinimumLength = 3)] string? Title);
