using MessagePack;

namespace HelpDeskLive.Application.Chat;

[MessagePackObject]
public sealed record ChatMessageDto(
    [property: Key(0)] int Id,
    [property: Key(1)] int TicketId,
    [property: Key(2)] int SenderId,
    [property: Key(3)] string SenderName,
    [property: Key(4)] string Content,
    [property: Key(5)] DateTime SentAt
);

[MessagePackObject]
public sealed record ChatMessagesResultDto(
    [property: Key(0)] int TicketId,
    [property: Key(1)] string Status,
    [property: Key(2)] int? AssignedAgentId,
    [property: Key(3)] bool WaitingForAgent,
    [property: Key(4)] IReadOnlyList<ChatMessageDto> Messages
);
