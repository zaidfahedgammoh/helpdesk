
using HelpDeskLive.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HelpDeskLive.API.Authentication;

public sealed class PasswordService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public bool VerifyPassword(User user, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        return result != PasswordVerificationResult.Failed;
    }
}
