using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Infrastructure.Repositories;

[Collection("Integration")]
public sealed class RefreshTokenRepositoryTests(DatabaseFixture fixture)
{
    private static User CreateUser() => new(
        Guid.NewGuid(),
        TestData.Email(),
        "Integration User",
        TestData.ShortString(32),
        UserRole.Admin,
        DateTime.Now,
        DateTime.Now
    );

    private static RefreshToken CreateRefreshToken(Guid userId, DateTime? expiresAt = null) => new(
        Guid.NewGuid(),
        userId,
        Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
        expiresAt ?? DateTime.Now.AddDays(7),
        DateTime.Now,
        DateTime.Now
    );

    private async Task WithScopeAsync(Func<IRefreshTokenRepository, Task> action)
    {
        using var scope = fixture.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>());
    }

    private async Task<User> SeedUserAsync()
    {
        var user = CreateUser();

        using var scope = fixture.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        await userRepository.AddAsync(user, CancellationToken.None);
        await userRepository.UnitOfWork.CommitAsync(CancellationToken.None);

        return user;
    }

    private async Task SeedAsync(params RefreshToken[] refreshTokens) => await WithScopeAsync(async repo =>
    {
        foreach (var refreshToken in refreshTokens)
            await repo.AddAsync(refreshToken, CancellationToken.None);

        await repo.UnitOfWork.CommitAsync(CancellationToken.None);
    });

    [Fact(DisplayName = "RefreshTokenRepository >> Should persist and retrieve >> When adding a new refresh token")]
    public async Task RefreshTokenRepository_ShouldPersistAndRetrieve_WhenAddingNewRefreshToken()
    {
        // Arrange
        var user = await SeedUserAsync();
        var refreshToken = CreateRefreshToken(user.Id);

        // Act
        await WithScopeAsync(async repo =>
        {
            await repo.AddAsync(refreshToken, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        RefreshToken? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(refreshToken.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(refreshToken.Id, found!.Id);
        Assert.Equal(refreshToken.UserId, found.UserId);
        Assert.Equal(refreshToken.TokenHash, found.TokenHash);
        Assert.Equal(refreshToken.ExpiresAt, found.ExpiresAt);
        Assert.Null(found.RevokedAt);
        Assert.Null(found.ReplacedByTokenId);
    }

    [Fact(DisplayName = "RefreshTokenRepository >> Should find by token hash >> When refresh token exists")]
    public async Task RefreshTokenRepository_ShouldFindByTokenHash_WhenRefreshTokenExists()
    {
        // Arrange
        var user = await SeedUserAsync();
        var refreshToken = CreateRefreshToken(user.Id);
        await SeedAsync(refreshToken);

        // Act
        RefreshToken? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByTokenHashAsync(refreshToken.TokenHash, CancellationToken.None));

        // Assert
        Assert.NotNull(found);
        Assert.Equal(refreshToken.Id, found!.Id);
    }

    [Fact(DisplayName = "RefreshTokenRepository >> Should return null >> When token hash does not exist")]
    public async Task RefreshTokenRepository_ShouldReturnNull_WhenTokenHashDoesNotExist()
    {
        // Arrange
        var tokenHash = Guid.NewGuid().ToString("N");

        // Act
        RefreshToken? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByTokenHashAsync(tokenHash, CancellationToken.None));

        // Assert
        Assert.Null(found);
    }

    [Fact(DisplayName = "RefreshTokenRepository >> Should persist revocation >> When updating a revoked refresh token")]
    public async Task RefreshTokenRepository_ShouldPersistRevocation_WhenUpdatingRevokedRefreshToken()
    {
        // Arrange
        var user = await SeedUserAsync();
        var refreshToken = CreateRefreshToken(user.Id);
        await SeedAsync(refreshToken);

        var replacedByTokenId = Guid.NewGuid();
        refreshToken.Revoke(replacedByTokenId);

        // Act
        await WithScopeAsync(async repo =>
        {
            repo.Update(refreshToken);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        RefreshToken? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(refreshToken.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.True(found!.IsRevoked);
        Assert.Equal(refreshToken.RevokedAt, found.RevokedAt);
        Assert.Equal(replacedByTokenId, found.ReplacedByTokenId);
    }

    [Fact(DisplayName = "RefreshTokenRepository >> Should return only active tokens of the user >> When user has active, revoked and expired tokens")]
    public async Task RefreshTokenRepository_ShouldReturnOnlyActiveTokensOfTheUser_WhenUserHasActiveRevokedAndExpiredTokens()
    {
        // Arrange
        var user = await SeedUserAsync();
        var otherUser = await SeedUserAsync();

        var active = CreateRefreshToken(user.Id);
        var revoked = CreateRefreshToken(user.Id);
        revoked.Revoke();
        var expired = CreateRefreshToken(user.Id, DateTime.Now.AddMinutes(-1));
        var otherUserActive = CreateRefreshToken(otherUser.Id);

        await SeedAsync(active, revoked, expired, otherUserActive);

        // Act
        IReadOnlyCollection<RefreshToken> found = [];
        await WithScopeAsync(async repo => found = await repo.GetAllActiveByUserIdAsync(user.Id, CancellationToken.None));

        // Assert
        var single = Assert.Single(found);
        Assert.Equal(active.Id, single.Id);
    }

    [Fact(DisplayName = "RefreshTokenRepository >> Should remove entity >> When removing an existing refresh token")]
    public async Task RefreshTokenRepository_ShouldRemoveEntity_WhenRemovingExistingRefreshToken()
    {
        // Arrange
        var user = await SeedUserAsync();
        var refreshToken = CreateRefreshToken(user.Id);
        await SeedAsync(refreshToken);

        // Act
        await WithScopeAsync(async repo =>
        {
            repo.Remove(refreshToken);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        RefreshToken? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(refreshToken.Id, CancellationToken.None));

        Assert.Null(found);
    }

    [Fact(DisplayName = "RefreshTokenRepository >> Should delete user tokens >> When the user is removed")]
    public async Task RefreshTokenRepository_ShouldDeleteUserTokens_WhenTheUserIsRemoved()
    {
        // Arrange
        var user = await SeedUserAsync();
        var refreshToken = CreateRefreshToken(user.Id);
        await SeedAsync(refreshToken);

        // Act
        using (var scope = fixture.Services.CreateScope())
        {
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            userRepository.Remove(user);
            await userRepository.UnitOfWork.CommitAsync(CancellationToken.None);
        }

        // Assert
        RefreshToken? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(refreshToken.Id, CancellationToken.None));

        Assert.Null(found);
    }
}
