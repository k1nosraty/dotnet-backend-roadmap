using System.ComponentModel.DataAnnotations;
namespace SupportDesk.Contracts;
public sealed class ChangeStatusRequest
{
    [Required]
    public string? Status { get; init; }
}
