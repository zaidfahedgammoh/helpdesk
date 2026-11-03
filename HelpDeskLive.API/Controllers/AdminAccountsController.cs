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
    public async Task<IActionResult> Get(string role, CancellationToken cancellationToken)
    {
        if (!TryRole(role, out var parsedRole))
            return BadRequest();

        var accounts = await _db.Users.AsNoTracking()
            .Where(user => user.Role == parsedRole)
            .OrderBy(user => user.Name)
            .Select(user => new AccountDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.DepartmentId))
            .ToListAsync(cancellationToken);

        return Ok(accounts);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        if (!TryRole(request.Role, out var parsedRole))
            return BadRequest(new { message = "Only Customer and Agent roles can be created here." });

        if (string.IsNullOrWhiteSpace(request.Name) ||
            !System.Net.Mail.MailAddress.TryCreate(request.Email, out _) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8)
            return BadRequest(new { message = "Enter a name, valid email, and password of at least 8 characters." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(user => user.Email == email, cancellationToken))
            return Conflict(new { message = "Email already exists." });

        if (parsedRole == UserRole.Agent && (request.DepartmentId is null || !await _db.Departments.AnyAsync(d => d.Id == request.DepartmentId, cancellationToken)))\n            return BadRequest(new { message = "Select a valid department for the agent." });\n        if (parsedRole == UserRole.Customer && request.DepartmentId is not null)\n            return BadRequest();\n\n        var temporary = new User(request.Name, email, "temporary-hash", parsedRole, PreferredLanguage.English, request.DepartmentId);
        var hash = _hasher.HashPassword(temporary, request.Password);
        var account = new User(request.Name, email, hash, parsedRole, PreferredLanguage.English, request.DepartmentId);

        await _db.Users.AddAsync(account, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Created($"/api/admin/accounts/{account.Id}",
            new AccountDto(account.Id, account.Name, account.Email, account.Role.ToString(), account.DepartmentId));
    }

    private static bool TryRole(string value, out UserRole role)
    {
        if (string.Equals(value, "Customer", StringComparison.OrdinalIgnoreCase))
        {
            role = UserRole.Customer;
            return true;
        }
        if (string.Equals(value, "Agent", StringComparison.OrdinalIgnoreCase))
        {
            role = UserRole.Agent;
            return true;
        }
        role = default;
        return false;
    }

    public sealed record CreateAccountRequest(string Name, string Email, string Password, string Role, int? DepartmentId);
    public sealed record AccountDto(int Id, string Name, string Email, string Role, int? DepartmentId);
}
