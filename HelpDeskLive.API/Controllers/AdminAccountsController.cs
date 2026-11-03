using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/admin/accounts")]
[Authorize(Roles = "Admin")]
public sealed class AdminAccountsController : ControllerBase
{
    private readonly HelpDeskDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();
    public AdminAccountsController(HelpDeskDbContext db) => _db = db;

    [HttpGet("{role}")]
    public async Task<IActionResult> Get(string role, CancellationToken ct)
    {
        if (!TryRole(role, out var parsedRole)) return BadRequest();
        var accounts = await _db.Users.AsNoTracking()
            .Where(u => u.Role == parsedRole).OrderBy(u => u.Name)
            .Select(u => new AccountDto(u.Id, u.Name, u.Email, u.Role.ToString(), u.DepartmentId))
            .ToListAsync(ct);
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
            return BadRequest(new { message = "Enter a name, valid email, and password of at least 8 characters." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new { message = "Email already exists." });

        if (role == UserRole.Agent &&
            (request.DepartmentId is null || !await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, ct)))
            return BadRequest(new { message = "Select a valid department for the agent." });
        if (role == UserRole.Customer && request.DepartmentId is not null)
            return BadRequest();

        var temporary = new User(request.Name, email, "temporary-hash", role, PreferredLanguage.English, request.DepartmentId);
        var hash = _hasher.HashPassword(temporary, request.Password);
        var account = new User(request.Name, email, hash, role, PreferredLanguage.English, request.DepartmentId);
        await _db.Users.AddAsync(account, ct);
        await _db.SaveChangesAsync(ct);
        return Created($"/api/admin/accounts/{account.Id}",
            new AccountDto(account.Id, account.Name, account.Email, account.Role.ToString(), account.DepartmentId));
    }

    [HttpPut("{id:int}/department")]
    public async Task<IActionResult> ChangeDepartment(int id, ChangeAgentDepartmentRequest request, CancellationToken ct)
    {
        var agent = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Agent, ct);
        if (agent is null) return NotFound();
        if (!await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId, ct)) return BadRequest();
        agent.ChangeDepartment(request.DepartmentId);
        await _db.SaveChangesAsync(ct);
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
    public sealed record AccountDto(int Id, string Name, string Email, string Role, int? DepartmentId);
}
