namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class RefreshTokenModel : Model
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshTokenModel() { }

    public RefreshTokenModel(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAt,
        DateTime? revokedAt,
        Guid? replacedByTokenId,
        DateTime createdAt,
        DateTime updatedAt
    ) : base(id, createdAt, updatedAt)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        RevokedAt = revokedAt;
        ReplacedByTokenId = replacedByTokenId;
    }
}
