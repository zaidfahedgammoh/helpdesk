
using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.Domain.Entities;

public class User
{
    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public PreferredLanguage PreferredLanguage { get; private set; }

    private User()
    {
    }

    public User(
        string name,
        string email,
        string passwordHash,
        UserRole role,
        PreferredLanguage preferredLanguage)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        PreferredLanguage = preferredLanguage;
    }
}
