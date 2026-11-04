using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public sealed class ChatController : ControllerBase
{
    private readonly HelpDeskDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ChatController(HelpDeskDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("{ticketId:int}/messages")]
    public async Task<IActionResult> Messages(int ticketId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int userId) return Unauthorized();

        var ticket = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is null) return NotFound();
        var isCustomer = _currentUser.Role == UserRole.Customer.ToString() && ticket.CustomerId == userId;
        var isAssignedAgent = _currentUser.Role == UserRole.Agent.ToString() && ticket.AssignedAgentId == userId;
        if (!isCustomer && !isAssignedAgent) return Forbid();

        if (ticket.ApprovalStatus != TicketApprovalStatus.Approved ||
            ticket.Priority is not (TicketPriority.High or TicketPriority.Critical))
            return Conflict(new { message = "Live chat is available only for approved High and Critical tickets." });

        var messages = await (
            from message in _db.ChatMessages.AsNoTracking()
            join user in _db.Users.AsNoTracking() on message.SenderId equals user.Id
            where message.TicketId == ticketId
            orderby message.SentAt
            select new
            {
                id = message.Id,
                ticketId = message.TicketId,
                senderId = message.SenderId,
                senderName = user.Name,
                content = message.Content,
                sentAt = message.SentAt
            }).ToListAsync(ct);

        return Ok(new
        {
            ticketId,
            status = ticket.Status.ToString(),
            assignedAgentId = ticket.AssignedAgentId,
            waitingForAgent = ticket.AssignedAgentId is null,
            messages
        });
    }
}
