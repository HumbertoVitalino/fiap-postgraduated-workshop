using Fiap.Workshop.Domain.Abstractions;

namespace Fiap.Workshop.Domain.Entities;

public class RefreshToken(
    Guid id,
    Guid userId,
    string tokenHash,
    DateTime expiresAt,
    DateTime createdAt,
    DateTime updatedAt,
    DateTime? revokedAt = null,
    Guid? replacedByTokenId = null
) : AggregateRoot(id, createdAt, updatedAt)
{
    public Guid UserId { get; private set; } = userId;
    public string TokenHash { get; private set; } = tokenHash;
    public DateTime ExpiresAt { get; private set; } = expiresAt;
    public DateTime? RevokedAt { get; private set; } = revokedAt;
    public Guid? ReplacedByTokenId { get; private set; } = replacedByTokenId;

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive() => !IsRevoked && DateTime.Now < ExpiresAt;

    public void Revoke(Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
            return;

        RevokedAt = DateTime.Now;
        ReplacedByTokenId = replacedByTokenId;
        SetUpdatedAt();
    }
}
