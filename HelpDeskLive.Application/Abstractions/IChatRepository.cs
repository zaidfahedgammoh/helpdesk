using HelpDeskLive.Domain.Entities;

namespace HelpDeskLive.Application.Abstractions;

public interface IChatRepository
{
    Task<IReadOnlyList<ChatMessage>> GetMessagesByTicketIdAsync(int ticketId, CancellationToken ct = default);
    Task AddMessageAsync(ChatMessage message, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
