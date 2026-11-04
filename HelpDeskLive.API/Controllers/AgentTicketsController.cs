using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/agent/tickets")]
[Authorize(Roles = "Agent")]
public sealed class AgentTicketsController : ControllerBase
{
    private readonly HelpDeskDbContext _db;
    private readonly ICurrentUser _currentUser;
    public AgentTicketsController(HelpDeskDbContext db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (_currentUser.UserId is not int agentId) return Unauthorized();
        var agent = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == agentId && u.Role == UserRole.Agent, ct);
        if (agent?.DepartmentId is not int departmentId) return Forbid();

        var rows = await _db.Tickets.AsNoTracking()
            .Where(t => t.DepartmentId == departmentId &&
                t.ApprovalStatus == TicketApprovalStatus.Approved &&
                t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved &&
                (t.AssignedAgentId == null || t.AssignedAgentId == agentId))
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new { t.Id, t.Title, t.Description, t.Status, t.Priority, t.DepartmentId, t.CreatedAt, t.AssignedAgentId })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("{ticketId:int}/start")]
    public async Task<IActionResult> Start(int ticketId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int agentId) return Unauthorized();
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var agent = await _db.Users.FirstOrDefaultAsync(u => u.Id == agentId && u.Role == UserRole.Agent, ct);
        if (agent?.DepartmentId is not int departmentId) return Forbid();

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId &&
            t.DepartmentId == departmentId && t.ApprovalStatus == TicketApprovalStatus.Approved &&
            t.Status == TicketStatus.Open &&
            (t.AssignedAgentId == null || t.AssignedAgentId == agentId), ct);
        if (ticket is null) return NotFound(new { message = "This ticket is no longer waiting in your department." });

        var isLivePriority = ticket.Priority is TicketPriority.High or TicketPriority.Critical;
        var activeLiveChat = await _db.Tickets.AnyAsync(t => t.AssignedAgentId == agentId &&
            t.Status == TicketStatus.InProgress && t.ApprovalStatus == TicketApprovalStatus.Approved &&
            (t.Priority == TicketPriority.High || t.Priority == TicketPriority.Critical), ct);
        if (activeLiveChat) return Conflict(new { message = "Finish your current live chat before starting another ticket." });

        var activeTickets = await _db.Tickets.CountAsync(t => t.AssignedAgentId == agentId &&
            t.Status == TicketStatus.InProgress && t.ApprovalStatus == TicketApprovalStatus.Approved, ct);
        if (activeTickets >= 5) return Conflict(new { message = "You have reached the five-ticket workload limit." });

        if (!isLivePriority)
        {
            var liveWaiting = await _db.Tickets.AnyAsync(t => t.DepartmentId == departmentId &&
                t.AssignedAgentId == null && t.Status == TicketStatus.Open &&
                t.ApprovalStatus == TicketApprovalStatus.Approved &&
                (t.Priority == TicketPriority.High || t.Priority == TicketPriority.Critical), ct);
            if (liveWaiting) return Conflict(new { message = "A high-priority customer is waiting for live support." });
        }

        if (ticket.AssignedAgentId is null) ticket.AssignToAgent(agentId);
        ticket.ChangeStatus(TicketStatus.InProgress);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { priority = (int?)ticket.Priority, isLiveChat = isLivePriority });
    }

    [HttpPost("{ticketId:int}/resolve")]
    public async Task<IActionResult> Resolve(int ticketId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int agentId) return Unauthorized();
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.AssignedAgentId == agentId, ct);
        if (ticket is null) return NotFound();
        ticket.ChangeStatus(TicketStatus.Resolved);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{ticketId:int}/close")]
    public async Task<IActionResult> Close(int ticketId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int agentId) return Unauthorized();
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.AssignedAgentId == agentId, ct);
        if (ticket is null) return NotFound();
        ticket.ChangeStatus(TicketStatus.Closed);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
