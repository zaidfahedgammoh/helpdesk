
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;


namespace HelpDeskLive.API.Services;

public sealed class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string GetRequiredSetting(string key)
    {
        return _configuration[key]
            ?? throw new InvalidOperationException(
                $"Missing configuration setting: {key}");
    }

    public string GenerateAccessToken(int userId, string role)
    {
        var issuer = GetRequiredSetting("Jwt:Issuer");
        var audience = GetRequiredSetting("Jwt:Audience");
        var signingKey = GetRequiredSetting("Jwt:SigningKey");

        var tokenMinutesValue =
            GetRequiredSetting("Jwt:AccessTokenMinutes");

        if (!double.TryParse(
                tokenMinutesValue,
                out var tokenMinutes)
            || tokenMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Jwt:AccessTokenMinutes must be a positive number.");
        }

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Role, role)
    };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(tokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
