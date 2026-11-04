namespace HelpDeskLive.MVC.Models;

public sealed class ChatPageViewModel
{
    public int TicketId { get; init; }
    public int CurrentUserId { get; init; }
    public string AccessToken { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = string.Empty;
    public bool IsAgent { get; init; }
    public int TicketStatus { get; init; } = 2;
}
