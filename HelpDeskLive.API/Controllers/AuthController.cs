
using HelpDeskLive.API.Authentication;
using HelpDeskLive.API.Models;
using HelpDeskLive.API.Services;
using HelpDeskLive.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordService _passwordService;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        IUserRepository userRepository,
        PasswordService passwordService,
        JwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(
            request.Email,
            cancellationToken);

        if (user is null ||
            !_passwordService.VerifyPassword(user, request.Password))
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.Id,
            user.Role.ToString());

        return Ok(new
        {
            accessToken
        });
    }
}
