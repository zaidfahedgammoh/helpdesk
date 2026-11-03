using System.ComponentModel.DataAnnotations;

namespace HelpDeskLive.API.Models;

public sealed class CreateTicketRequest
{
    public int? IssueTypeId { get; init; }

    [StringLength(200, MinimumLength = 3)]
    public string? CustomTitle { get; init; }

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Description { get; init; } = string.Empty;
}
