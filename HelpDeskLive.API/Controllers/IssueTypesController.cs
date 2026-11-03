using HelpDeskLive.Application.IssueTypes.CreateIssueType;
using HelpDeskLive.Application.IssueTypes.GetIssueTypes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/issue-types")]
[Authorize]
public sealed class IssueTypesController : ControllerBase
{
    private readonly ISender _sender;
    public IssueTypesController(ISender sender) => _sender = sender;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var issueTypes = await _sender.Send(new GetIssueTypesQuery(), cancellationToken);
        return Ok(issueTypes);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateIssueTypeCommand command, CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);
        return Created($"/api/issue-types/{id}", new { id });
    }
}