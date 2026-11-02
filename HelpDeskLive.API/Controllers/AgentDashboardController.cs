
using HelpDeskLive.Application.Dashboard.Agent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/agent/dashboard")]
[Authorize(Roles = "Agent")]
public sealed class AgentDashboardController : ControllerBase
{
    private readonly ISender _sender;

    public AgentDashboardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetAgentDashboardQuery(),
            cancellationToken);

        return Ok(result);
    }
}
