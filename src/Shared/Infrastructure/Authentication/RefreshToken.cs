using Microsoft.AspNetCore.Identity;

namespace Shared.Infrastructure.Authentication;

/// <summary>
/// Refresh Tokenエンティティ
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public string Token { get; private set; } = default!;
    public string UserId { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // EF Core用
    private RefreshToken() { }

    public static RefreshToken Create(
        string token,
        string userId,
        DateTime expiresAt)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            UserId = userId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
    }

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    public bool IsValid()
    {
        return !IsRevoked && ExpiresAt > DateTime.UtcNow;
    }
}
