using System.Security.Cryptography;
using System.Text;
using HelpDeskLive.API.Authentication;
using HelpDeskLive.API.Models;
using HelpDeskLive.API.Services;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    private const string RefreshCookieName = "helpdesk_refresh";
    private readonly IUserRepository _userRepository;
    private readonly PasswordService _passwordService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly HelpDeskDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public AuthController(
        IUserRepository userRepository,
        PasswordService passwordService,
        JwtTokenService jwtTokenService,
        HelpDeskDbContext db,
        IWebHostEnvironment environment)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _db = db;
        _environment = environment;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordService.VerifyPassword(user, request.Password))
            return Unauthorized(new { message = "Invalid email or password." });

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Role.ToString());
        SetRefreshCookie(await IssueRefreshTokenAsync(user.Id, cancellationToken));
        return Ok(new { accessToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookieName, out var rawToken) ||
            string.IsNullOrWhiteSpace(rawToken))
            return Unauthorized(new { message = "A valid refresh session is required." });

        var tokenHash = HashToken(rawToken);
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            ClearRefreshCookie();
            return Unauthorized(new { message = "The refresh session is invalid or expired." });
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == storedToken.UserId, cancellationToken);
        if (user is null)
        {
            storedToken.Revoke();
            await _db.SaveChangesAsync(cancellationToken);
            ClearRefreshCookie();
            return Unauthorized();
        }

        storedToken.Revoke();
        var newRawToken = await IssueRefreshTokenAsync(user.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        SetRefreshCookie(newRawToken);

        return Ok(new
        {
            accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Role.ToString())
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshCookieName, out var rawToken) &&
            !string.IsNullOrWhiteSpace(rawToken))
        {
            var tokenHash = HashToken(rawToken);
            var storedToken = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == tokenHash, cancellationToken);
            storedToken?.Revoke();
            if (storedToken is not null)
                await _db.SaveChangesAsync(cancellationToken);
        }

        ClearRefreshCookie();
        return NoContent();
    }

    private async Task<string> IssueRefreshTokenAsync(int userId, CancellationToken cancellationToken)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        _db.RefreshTokens.Add(new RefreshToken(
            userId, HashToken(rawToken), DateTime.UtcNow.Add(RefreshLifetime)));
        await _db.SaveChangesAsync(cancellationToken);
        return rawToken;
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private void SetRefreshCookie(string token)
    {
        Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment() || Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.Add(RefreshLifetime),
            IsEssential = true,
            Path = "/"
        });
    }

    private void ClearRefreshCookie() =>
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment() || Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
}