using System.Security.Claims;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Chat;
using HelpDeskLive.Application.Departments;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace HelpDeskLive.API.Hubs;

[Authorize]
public sealed class TicketChatHub : Hub
{
    private readonly ISender _sender;
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<TicketChatHub> _logger;

    public TicketChatHub(
        ISender sender,
        ITicketRepository ticketRepository,
        ILogger<TicketChatHub> logger)
    {
        _sender = sender;
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        if (role == UserRole.Agent.ToString() && int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var agentId))
        {
            var departmentId = await _sender.Send(new GetAgentDepartmentQuery(agentId));
            if (departmentId.HasValue)
                await Groups.AddToGroupAsync(Context.ConnectionId, DepartmentGroupName(departmentId.Value));
        }
        await base.OnConnectedAsync();
    }

    public async Task JoinTicket(int ticketId)
    {
        var userId = GetUserId();
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null || !CanAccess(ticket, userId))
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

    [Authorize(Roles = "Agent")]
    public async Task JoinDepartment(int departmentId)
    {
        var agentId = GetUserId();
        var agentDeptId = await _sender.Send(new GetAgentDepartmentQuery(agentId));
        if (agentDeptId is null || agentDeptId.Value != departmentId)
            throw new HubException("You do not belong to this department.");

        await Groups.AddToGroupAsync(Context.ConnectionId, DepartmentGroupName(departmentId));
    }

    [Authorize(Roles = "Agent")]
    public async Task JoinMyDepartment()
    {
        var agentId = GetUserId();
        var agentDeptId = await _sender.Send(new GetAgentDepartmentQuery(agentId));
        if (agentDeptId is not int departmentId)
            throw new HubException("You do not have an assigned department.");

        await Groups.AddToGroupAsync(Context.ConnectionId, DepartmentGroupName(departmentId));
    }

    [Authorize(Roles = "Agent")]
    public async Task LeaveDepartment(int departmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, DepartmentGroupName(departmentId));
    }

    public async Task UserTyping(int ticketId)
    {
        var userId = GetUserId();
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null || !CanAccess(ticket, userId) || ticket.Status == TicketStatus.Closed)
            return;

        await Clients.OthersInGroup(GroupName(ticketId)).SendAsync("UserTyping", new { userId });
    }

    public async Task StopTyping(int ticketId)
    {
        var userId = GetUserId();
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null || !CanAccess(ticket, userId) || ticket.Status == TicketStatus.Closed)
            return;

        await Clients.OthersInGroup(GroupName(ticketId)).SendAsync("StopTyping", new { userId });
    }

    public async Task<ChatMessageDto> SendMessage(int ticketId, string content)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(content) || content.Trim().Length > 5000)
            throw new HubException("Messages must contain between 1 and 5000 characters.");

        try
        {
            var messageDto = await _sender.Send(new SendChatMessageCommand(ticketId, userId, content));
            await Clients.Group(GroupName(ticketId)).SendAsync("ReceiveMessage", messageDto);
            return messageDto;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business rule violation when sending message on ticket #{TicketId} by user #{UserId}", ticketId, userId);
            throw new HubException(ex.Message);
        }
        catch (Exception ex) when (ex is not HubException)
        {
            _logger.LogError(ex, "Unexpected error delivering chat message on ticket #{TicketId}", ticketId);
            throw new HubException("The message could not be sent. Please check your connection and try again.");
        }
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
            : role == UserRole.Agent.ToString() && (ticket.AssignedAgentId == userId || ticket.AssignedAgentId == null)
            || role == UserRole.Admin.ToString();
    }

    private static string GroupName(int ticketId) => $"ticket-{ticketId}";
    private static string DepartmentGroupName(int departmentId) => $"department-{departmentId}";
}
