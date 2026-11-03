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
    public int? DepartmentId { get; private set; }

    private User() { }

    public User(string name, string email, string passwordHash, UserRole role, PreferredLanguage preferredLanguage, int? departmentId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        if (!System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var address) ||
            !string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A valid email address is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        if (!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role));
        if (!Enum.IsDefined(preferredLanguage)) throw new ArgumentOutOfRangeException(nameof(preferredLanguage));
        if (role == UserRole.Agent && departmentId is null)
            throw new ArgumentException("An agent must belong to a department.", nameof(departmentId));
        if (role != UserRole.Agent && departmentId is not null)
            throw new ArgumentException("Only agents can be assigned to a department.", nameof(departmentId));

        Name = name.Trim();
        Email = address.Address.ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        PreferredLanguage = preferredLanguage;
        DepartmentId = departmentId;
    }

    public void ChangeDepartment(int departmentId)
    {
        if (Role != UserRole.Agent) throw new InvalidOperationException("Only agents can be assigned to departments.");
        if (departmentId <= 0) throw new ArgumentOutOfRangeException(nameof(departmentId));
        DepartmentId = departmentId;
    }
}
