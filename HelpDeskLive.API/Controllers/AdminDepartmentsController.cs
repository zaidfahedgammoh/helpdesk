using HelpDeskLive.Application.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/admin/departments")]
[Authorize(Roles = "Admin")]
public sealed class AdminDepartmentsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminDepartmentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var departments = await _sender.Send(new GetDepartmentsQuery(), cancellationToken);
        return Ok(departments);
    }
}
