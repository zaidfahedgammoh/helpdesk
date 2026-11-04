namespace HelpDeskLive.Domain.Enums;

public enum TicketStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4,
    // Escalated is a queue state, not a backward transition to Open.
    Escalated = 5
}
