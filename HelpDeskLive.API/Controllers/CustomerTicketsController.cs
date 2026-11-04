using HelpDeskLive.API.Hubs;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Tickets.Customer;
using HelpDeskLive.Application.Tickets.GetMyTickets;
using HelpDeskLive.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/customer/tickets")]
[Authorize(Roles = "Customer")]
public sealed class CustomerTicketsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;
    private readonly IHubContext<TicketChatHub> _chatHub;

    public CustomerTicketsController(
        ISender sender,
        ICurrentUser currentUser,
        IHubContext<TicketChatHub> chatHub)
    {
        _sender = sender;
        _currentUser = currentUser;
        _chatHub = chatHub;
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

        try
        {
            await _sender.Send(new CloseCustomerTicketCommand(ticketId, customerId), ct);

            await _chatHub.Clients.Group($"ticket-{ticketId}").SendAsync(
                "TicketStatusChanged",
                new { ticketId, status = TicketStatus.Closed.ToString() },
                ct);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Conflict", Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not Found", Detail = ex.Message });
        }
    }
}
