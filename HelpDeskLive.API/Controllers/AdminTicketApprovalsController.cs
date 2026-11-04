using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/admin/ticket-approvals")]
[Authorize(Roles = "Admin")]
public sealed class AdminTicketApprovalsController : ControllerBase
{
    private readonly HelpDeskDbContext _db;
    public AdminTicketApprovalsController(HelpDeskDbContext db) => _db = db;

    [HttpGet("pending")]
    public async Task<IActionResult> Pending(CancellationToken ct)
    {
        var rows = await _db.Tickets.AsNoTracking()
            .Where(t => t.ApprovalStatus == TicketApprovalStatus.Pending)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new { t.Id, t.Title, t.Description, t.CustomerId, t.CreatedAt })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("{ticketId:int}/approve")]
    public async Task<IActionResult> Approve(int ticketId, ReviewTicketRequest request, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is null) return NotFound();
        var departmentExists = await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId, ct);
        if (!departmentExists) return BadRequest(new { message = "Select a valid department." });

        ticket.Approve(request.Priority, request.DepartmentId);


        await _db.SaveChangesAsync(ct);
        return Ok(new { ticket.Id, AssignedAgentId = (int?)null });
    }

    [HttpPost("{ticketId:int}/reject")]
    public async Task<IActionResult> Reject(int ticketId, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is null) return NotFound();
        ticket.Reject();
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    public sealed record ReviewTicketRequest(TicketPriority Priority, int DepartmentId);
}
