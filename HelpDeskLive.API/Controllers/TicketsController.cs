
using HelpDeskLive.API.Models;
using HelpDeskLive.Application.Tickets.CreateTicket;
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

    public TicketsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create(
        [FromBody] CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTicketCommand(
            request.Title,
            request.Description,
            request.Priority,
            request.DepartmentId);

        var ticketId = await _sender.Send(
            command,
            cancellationToken);

        return Created(
            $"/api/tickets/{ticketId}",
            new { id = ticketId });
    }
}
