
namespace HelpDeskLive.Domain.Entities;

public class ChatMessage
{
    public int Id { get; private set; }
    public int TicketId { get; private set; }
    public int SenderId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; }
    public bool IsRead { get; private set; }
    private ChatMessage() { }

    public ChatMessage(int ticketId, int senderId, string content)
    {
        if (ticketId <= 0)
            throw new ArgumentOutOfRangeException(nameof(ticketId));

        if (senderId <= 0)
            throw new ArgumentOutOfRangeException(nameof(senderId));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException(
                "Message content is required.", nameof(content));

        TicketId = ticketId;
        SenderId = senderId;
        Content = content.Trim();
        SentAt = DateTime.UtcNow;
        IsRead = false;
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
