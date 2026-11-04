using MessagePack;

namespace HelpDeskLive.Application.Common.Models;

[MessagePackObject]
public sealed record TicketClosedNotificationDto(
    [property: Key(0)] int TicketId,
    [property: Key(1)] string Title,
    [property: Key(2)] int CustomerId,
    [property: Key(3)] int? AgentId,
    [property: Key(4)] DateTime ClosedAt
);
