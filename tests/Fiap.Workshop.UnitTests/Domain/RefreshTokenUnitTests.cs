using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class RefreshTokenUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "RefreshToken >> Should be created >> When all required properties are provided")]
    public void RefreshToken_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tokenHash = _fixture.Create<string>();
        var expiresAt = DateTime.Now.AddDays(7);
        var createdAt = DateTime.Now;
        var updatedAt = DateTime.Now;
        var revokedAt = DateTime.Now;
        var replacedByTokenId = Guid.NewGuid();

        // Act
        var refreshToken = new RefreshToken(
            id,
            userId,
            tokenHash,
            expiresAt,
            createdAt,
            updatedAt,
            revokedAt,
            replacedByTokenId
        );

        // Assert
        Assert.Equal(id, refreshToken.Id);
        Assert.Equal(userId, refreshToken.UserId);
        Assert.Equal(tokenHash, refreshToken.TokenHash);
        Assert.Equal(expiresAt, refreshToken.ExpiresAt);
        Assert.Equal(createdAt, refreshToken.CreatedAt);
        Assert.Equal(updatedAt, refreshToken.UpdatedAt);
        Assert.Equal(revokedAt, refreshToken.RevokedAt);
        Assert.Equal(replacedByTokenId, refreshToken.ReplacedByTokenId);
    }

    [Fact(DisplayName = "RefreshToken >> Should use default values >> When optional properties are not provided")]
    public void RefreshToken_ShouldUseDefaultValues_WhenOptionalPropertiesAreNotProvided()
    {
        // Act
        var refreshToken = CreateRefreshToken(DateTime.Now.AddDays(7));

        // Assert
        Assert.Null(refreshToken.RevokedAt);
        Assert.Null(refreshToken.ReplacedByTokenId);
        Assert.False(refreshToken.IsRevoked);
    }

    [Fact(DisplayName = "RefreshToken >> Should be active >> When not revoked and not expired")]
    public void RefreshToken_ShouldBeActive_WhenNotRevokedAndNotExpired()
    {
        // Arrange
        var refreshToken = CreateRefreshToken(DateTime.Now.AddDays(7));

        // Act & Assert
        Assert.True(refreshToken.IsActive());
    }

    [Fact(DisplayName = "RefreshToken >> Should not be active >> When expired")]
    public void RefreshToken_ShouldNotBeActive_WhenExpired()
    {
        // Arrange
        var refreshToken = CreateRefreshToken(DateTime.Now.AddMinutes(-1));

        // Act & Assert
        Assert.False(refreshToken.IsActive());
    }

    [Fact(DisplayName = "RefreshToken >> Should not be active >> When revoked")]
    public void RefreshToken_ShouldNotBeActive_WhenRevoked()
    {
        // Arrange
        var refreshToken = CreateRefreshToken(DateTime.Now.AddDays(7));

        // Act
        refreshToken.Revoke();

        // Assert
        Assert.False(refreshToken.IsActive());
    }

    [Fact(DisplayName = "RefreshToken >> Should revoke >> When Revoke is called with a replacement token")]
    public void RefreshToken_ShouldRevoke_WhenRevokeIsCalledWithAReplacementToken()
    {
        // Arrange
        var refreshToken = CreateRefreshToken(DateTime.Now.AddDays(7));
        var replacedByTokenId = Guid.NewGuid();
        var before = DateTime.Now;

        // Act
        refreshToken.Revoke(replacedByTokenId);

        // Assert
        Assert.True(refreshToken.IsRevoked);
        Assert.InRange(refreshToken.RevokedAt!.Value, before, DateTime.Now);
        Assert.Equal(replacedByTokenId, refreshToken.ReplacedByTokenId);
        Assert.InRange(refreshToken.UpdatedAt, before, DateTime.Now);
    }

    [Fact(DisplayName = "RefreshToken >> Should revoke without replacement >> When Revoke is called without a replacement token")]
    public void RefreshToken_ShouldRevokeWithoutReplacement_WhenRevokeIsCalledWithoutAReplacementToken()
    {
        // Arrange
        var refreshToken = CreateRefreshToken(DateTime.Now.AddDays(7));

        // Act
        refreshToken.Revoke();

        // Assert
        Assert.True(refreshToken.IsRevoked);
        Assert.Null(refreshToken.ReplacedByTokenId);
    }

    [Fact(DisplayName = "RefreshToken >> Should keep original revocation >> When Revoke is called on an already revoked token")]
    public void RefreshToken_ShouldKeepOriginalRevocation_WhenRevokeIsCalledOnAnAlreadyRevokedToken()
    {
        // Arrange
        var revokedAt = DateTime.Now.AddMinutes(-10);
        var updatedAt = DateTime.Now.AddMinutes(-10);
        var replacedByTokenId = Guid.NewGuid();
        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            DateTime.Now.AddDays(7),
            DateTime.Now.AddMinutes(-20),
            updatedAt,
            revokedAt,
            replacedByTokenId
        );

        // Act
        refreshToken.Revoke(Guid.NewGuid());

        // Assert
        Assert.Equal(revokedAt, refreshToken.RevokedAt);
        Assert.Equal(replacedByTokenId, refreshToken.ReplacedByTokenId);
        Assert.Equal(updatedAt, refreshToken.UpdatedAt);
    }

    private RefreshToken CreateRefreshToken(DateTime expiresAt) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            expiresAt,
            DateTime.Now,
            DateTime.Now
        );
}
