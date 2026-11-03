using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/agent/tickets")]
[Authorize(Roles = "Agent")]
public sealed class AgentTicketsController : ControllerBase
{
    private readonly HelpDeskDbContext _db;
    private readonly ICurrentUser _currentUser;
    public AgentTicketsController(HelpDeskDbContext db, ICurrentUser currentUser) { _db = db; _currentUser = currentUser; }

    [HttpPost("{ticketId:int}/start")]
    public async Task<IActionResult> Start(int ticketId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int agentId) return Unauthorized();
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.AssignedAgentId == agentId, ct);
        if (ticket is null) return NotFound();
        var activeCount = await _db.Tickets.CountAsync(t => t.AssignedAgentId == agentId && t.Status == TicketStatus.InProgress && t.ApprovalStatus == TicketApprovalStatus.Approved, ct);
        if (activeCount >= 5) return Conflict(new { message = "An agent cannot handle more than five InProgress tickets." });
        ticket.ChangeStatus(TicketStatus.InProgress);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{ticketId:int}/resolve")]
    public async Task<IActionResult> Resolve(int ticketId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int agentId) return Unauthorized();
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.AssignedAgentId == agentId, ct);
        if (ticket is null) return NotFound();
        var departmentId = ticket.DepartmentId;
        ticket.ChangeStatus(TicketStatus.Resolved);
        await _db.SaveChangesAsync(ct);

        if (departmentId is int deptId)
        {
            var waiting = await _db.Tickets
                .Where(t => t.DepartmentId == deptId && t.AssignedAgentId == null &&
                    t.Status == TicketStatus.Open && t.ApprovalStatus == TicketApprovalStatus.Approved)
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (waiting is not null)
            {
                var available = await _db.Users
                    .Where(u => u.Role == UserRole.Agent && u.DepartmentId == deptId)
                    .Select(u => new { User = u, Load = _db.Tickets.Count(t => t.AssignedAgentId == u.Id && t.Status == TicketStatus.InProgress && t.ApprovalStatus == TicketApprovalStatus.Approved) })
                    .Where(x => x.Load < 5)
                    .OrderBy(x => x.Load).ThenBy(x => x.User.Id)
                    .FirstOrDefaultAsync(ct);
                if (available is not null) waiting.AssignToAgent(available.User.Id);
                await _db.SaveChangesAsync(ct);
            }
        }
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
