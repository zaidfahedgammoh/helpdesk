
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

    // EF Core uses this constructor when loading the entity.
    private User() { }

    public User(
        string name,
        string email,
        string passwordHash,
        UserRole role,
        PreferredLanguage preferredLanguage)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (!System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var address)
            || !string.Equals(address.Address, email.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A valid email address is required.",
                nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.",
                nameof(passwordHash));

        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role));

        if (!Enum.IsDefined(preferredLanguage))
            throw new ArgumentOutOfRangeException(nameof(preferredLanguage));

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        PreferredLanguage = preferredLanguage;
    }
}
