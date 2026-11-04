using HelpDeskLive.Application.Tickets.Admin;
using HelpDeskLive.Application.Tickets.Escalation;
using HelpDeskLive.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/admin/ticket-approvals")]
[Authorize(Roles = "Admin")]
public sealed class AdminTicketApprovalsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminTicketApprovalsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> Pending(CancellationToken ct)
    {
        var rows = await _sender.Send(new GetPendingApprovalsQuery(), ct);
        return Ok(rows);
    }

    [HttpPost("{ticketId:int}/approve")]
    public async Task<IActionResult> Approve(int ticketId, ReviewTicketRequest request, CancellationToken ct)
    {
        await _sender.Send(new ApproveTicketCommand(ticketId, request.Priority, request.DepartmentId), ct);
        return Ok(new { Id = ticketId, AssignedAgentId = (int?)null });
    }

    [HttpPost("{ticketId:int}/reject")]
    public async Task<IActionResult> Reject(int ticketId, CancellationToken ct)
    {
        await _sender.Send(new RejectTicketCommand(ticketId), ct);
        return NoContent();
    }

    [HttpGet("{ticketId:int}/escalations")]
    public async Task<IActionResult> Escalations(int ticketId, CancellationToken ct)
    {
        var history = await _sender.Send(new GetTicketEscalationHistoryQuery(ticketId), ct);
        return Ok(history);
    }

    public sealed record ReviewTicketRequest(TicketPriority Priority, int DepartmentId);
}
