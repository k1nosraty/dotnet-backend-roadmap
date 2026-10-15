using System.ComponentModel.DataAnnotations;
namespace SupportDesk.Contracts;
public sealed record ChangeStatusRequest([property: Required] string? Status);
