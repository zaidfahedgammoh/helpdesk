using HelpDeskLive.Application.Tickets.GetMyTickets;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/customer/tickets")]
[Authorize(Roles = "Customer")]
public sealed class CustomerTicketsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly HelpDeskDbContext _db;
    private readonly ICurrentUser _currentUser;
    public CustomerTicketsController(ISender sender, HelpDeskDbContext db, ICurrentUser currentUser)
    {
        _sender = sender;
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var tickets = await _sender.Send(new GetMyTicketsQuery(), cancellationToken);
        return Ok(tickets);
    }

    [HttpPost("{ticketId:int}/close")]
    public async Task<IActionResult> Close(int ticketId, CancellationToken ct)
    {
        if (_currentUser.UserId is not int customerId) return Unauthorized();
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && t.CustomerId == customerId, ct);
        if (ticket is null) return NotFound();
        if (ticket.ApprovalStatus != TicketApprovalStatus.Approved || ticket.Status == TicketStatus.Closed)
            return Conflict(new { message = "This ticket cannot be closed." });
        ticket.CloseByCustomer();
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}