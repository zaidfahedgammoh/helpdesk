using System.Security.Claims;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.API.Hubs;

[Authorize]
public sealed class TicketChatHub : Hub
{
    private readonly HelpDeskDbContext _db;

    public TicketChatHub(HelpDeskDbContext db) => _db = db;

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        if (role == UserRole.Agent.ToString() && int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var agentId))
        {
            var departmentId = await _db.Users.AsNoTracking()
                .Where(user => user.Id == agentId && user.Role == UserRole.Agent)
                .Select(user => user.DepartmentId)
                .FirstOrDefaultAsync();
            if (departmentId.HasValue)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"department-{departmentId.Value}");
        }
        await base.OnConnectedAsync();
    }

    public async Task JoinTicket(int ticketId)
    {
        var userId = GetUserId();
        var ticket = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket is null || !CanAccess(ticket, userId) || !IsChatTicket(ticket))
            throw new HubException("You are not allowed to join this ticket chat.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(ticketId));
        var role = Context.User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var participant = new { userId, role };
        await Clients.Group(GroupName(ticketId)).SendAsync("ChatParticipantJoined", participant);
        await Clients.Group(GroupName(ticketId)).SendAsync(
            role == UserRole.Agent.ToString() ? "AgentJoined" : "CustomerJoined",
            participant);
    }

    public async Task LeaveTicket(int ticketId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(ticketId));
    }

    public async Task UserTyping(int ticketId)
    {
        var userId = GetUserId();
        var ticket = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket is null || !CanAccess(ticket, userId) || !IsChatTicket(ticket))
            throw new HubException("You are not allowed to use this ticket chat.");

        await Clients.OthersInGroup(GroupName(ticketId)).SendAsync("UserTyping", new { userId });
    }

    public async Task StopTyping(int ticketId)
    {
        var userId = GetUserId();
        var ticket = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket is null || !CanAccess(ticket, userId) || !IsChatTicket(ticket))
            throw new HubException("You are not allowed to use this ticket chat.");

        await Clients.OthersInGroup(GroupName(ticketId)).SendAsync("StopTyping", new { userId });
    }

    public async Task SendMessage(int ticketId, string content)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(content) || content.Trim().Length > 5000)
            throw new HubException("Messages must contain between 1 and 5000 characters.");

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket is null || !CanAccess(ticket, userId) || !IsChatTicket(ticket))
            throw new HubException("You are not allowed to send messages in this chat.");

        var sender = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (sender is null) throw new HubException("Your account could not be found.");

        var message = new ChatMessage(ticketId, userId, content);
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        await Clients.Group(GroupName(ticketId)).SendAsync("ReceiveMessage", new
        {
            id = message.Id,
            ticketId,
            senderId = userId,
            senderName = sender.Name,
            content = message.Content,
            sentAt = message.SentAt
        });
    }

    private int GetUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var userId))
            throw new HubException("Your session is not valid. Please sign in again.");
        return userId;
    }

    private bool CanAccess(Ticket ticket, int userId)
    {
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        return role == UserRole.Customer.ToString()
            ? ticket.CustomerId == userId
            : role == UserRole.Agent.ToString() && ticket.AssignedAgentId == userId;
    }

    private static bool IsChatTicket(Ticket ticket) =>
        ticket.ApprovalStatus == TicketApprovalStatus.Approved &&
        (ticket.Priority is TicketPriority.High or TicketPriority.Critical) &&
        ticket.Status is TicketStatus.Open or TicketStatus.InProgress;

    private static string GroupName(int ticketId) => $"ticket-{ticketId}";
}
