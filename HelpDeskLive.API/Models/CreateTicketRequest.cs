
using System.ComponentModel.DataAnnotations;
using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.API.Models;

public sealed class CreateTicketRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Description { get; init; } = string.Empty;

    public TicketPriority Priority { get; init; }

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; init; }
}
