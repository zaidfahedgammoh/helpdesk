using System.ComponentModel.DataAnnotations;

namespace HelpDeskLive.API.Models;

public sealed class CreateTicketRequest
{
    [Range(1, int.MaxValue)]
    public int IssueTypeId { get; init; }

    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Description { get; init; } = string.Empty;
}