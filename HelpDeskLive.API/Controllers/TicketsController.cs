using HelpDeskLive.API.Models;
using HelpDeskLive.Application.Tickets.CreateTicket;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ITicketRepository _tickets;
    private readonly IHubContext<TicketChatHub> _hub;
    public TicketsController(ISender sender, ITicketRepository tickets, IHubContext<TicketChatHub> hub)
    { _sender = sender; _tickets = tickets; _hub = hub; }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var ticketId = await _sender.Send(
            new CreateTicketCommand(request.IssueTypeId, request.CustomTitle, request.Description),
            cancellationToken);
        var departmentId = await _tickets.GetDepartmentIdByTicketIdAsync(ticketId, cancellationToken);
        if (departmentId.HasValue)
            await _hub.Clients.Group($"department-{departmentId.Value}")
                .SendAsync("NewTicketAvailable", new { ticketId }, cancellationToken);
        return Created($"/api/tickets/{ticketId}", new { id = ticketId });
    }
}
