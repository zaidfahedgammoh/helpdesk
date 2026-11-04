using HelpDeskLive.API.Hubs;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Tickets.CreateTicket;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HelpDeskLive.API.Controllers;

public sealed record CreateTicketRequest(
    int? IssueTypeId,
    string? CustomTitle,
    string Description);

[ApiController]
[Route("api/[controller]")]
public sealed class TicketsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ITicketRepository _tickets;
    private readonly IHubContext<TicketChatHub> _hub;

    public TicketsController(ISender sender, ITicketRepository tickets, IHubContext<TicketChatHub> hub)
    {
        _sender = sender;
        _tickets = tickets;
        _hub = hub;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var ticketId = await _sender.Send(
            new CreateTicketCommand(request.IssueTypeId, request.CustomTitle, request.Description),
            cancellationToken);

        var ticket = await _tickets.GetByIdAsync(ticketId, cancellationToken);
        var departmentId = ticket?.DepartmentId;
        if (departmentId.HasValue)
        {
            await _hub.Clients.Group($"department-{departmentId.Value}")
                .SendAsync("NewTicketAvailable", new { ticketId }, cancellationToken);
        }

        return Created($"/api/tickets/{ticketId}", new { id = ticketId });
    }
}
