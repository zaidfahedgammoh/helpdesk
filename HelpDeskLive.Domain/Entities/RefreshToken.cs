
namespace HelpDeskLive.Domain.Entities;

public class RefreshToken
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private RefreshToken() { }

    public RefreshToken(int userId, string token, DateTime expiresAt)
    {
        if (userId <= 0)
            throw new ArgumentOutOfRangeException(nameof(userId));

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException(
                "Refresh token is required.", nameof(token));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt), "Expiration must be in the future.");

        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
    }

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke()
    {
        if (IsRevoked)
            return;

        RevokedAt = DateTime.UtcNow;
    }
}
