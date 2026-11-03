using HelpDeskLive.Application.Tickets.GetMyTickets;
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
    public CustomerTicketsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var tickets = await _sender.Send(new GetMyTicketsQuery(), cancellationToken);
        return Ok(tickets);
    }
}