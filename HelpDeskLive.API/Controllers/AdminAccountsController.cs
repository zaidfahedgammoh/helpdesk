using HelpDeskLive.Application.Admin;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/admin/accounts")]
[Authorize(Roles = "Admin")]
public sealed class AdminAccountsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly PasswordHasher<User> _hasher = new();

    public AdminAccountsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{role}")]
    public async Task<IActionResult> Get(string role, CancellationToken ct)
    {
        if (!TryRole(role, out var parsedRole)) return BadRequest();
        var accounts = await _sender.Send(new GetAccountsByRoleQuery(parsedRole), ct);
        return Ok(accounts);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest request, CancellationToken ct)
    {
        if (!TryRole(request.Role, out var role))
            return BadRequest(new { message = "Only Customer and Agent roles can be created here." });

        if (string.IsNullOrWhiteSpace(request.Name) ||
            !System.Net.Mail.MailAddress.TryCreate(request.Email, out _) ||
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest(new { message = "Enter a name, valid email, and password of at least 8 characters." });
        }

        var temporary = new User(request.Name, request.Email.Trim().ToLowerInvariant(), "temporary-hash", role, PreferredLanguage.English, request.DepartmentId);
        var hash = _hasher.HashPassword(temporary, request.Password);

        var account = await _sender.Send(new CreateAccountCommand(request.Name, request.Email, hash, role, request.DepartmentId), ct);

        return Created($"/api/admin/accounts/{account.Id}", account);
    }

    [HttpPut("{id:int}/department")]
    public async Task<IActionResult> ChangeDepartment(int id, ChangeAgentDepartmentRequest request, CancellationToken ct)
    {
        await _sender.Send(new ChangeAgentDepartmentCommand(id, request.DepartmentId), ct);
        return NoContent();
    }

    private static bool TryRole(string value, out UserRole role)
    {
        if (string.Equals(value, "Customer", StringComparison.OrdinalIgnoreCase)) { role = UserRole.Customer; return true; }
        if (string.Equals(value, "Agent", StringComparison.OrdinalIgnoreCase)) { role = UserRole.Agent; return true; }
        role = default; return false;
    }

    public sealed record CreateAccountRequest(string Name, string Email, string Password, string Role, int? DepartmentId);
    public sealed record ChangeAgentDepartmentRequest(int DepartmentId);
}
