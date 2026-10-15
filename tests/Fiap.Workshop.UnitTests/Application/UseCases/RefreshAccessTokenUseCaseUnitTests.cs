using AutoFixture;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.UseCases.Users.RefreshAccessToken;
using Fiap.Workshop.Application.UseCases.Users.RefreshAccessToken.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class RefreshAccessTokenUseCaseUnitTests : LoggerTestBase<RefreshAccessTokenUseCase>
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Fixture _fixture = new();
    private readonly RefreshAccessTokenUseCase _useCase;

    public RefreshAccessTokenUseCaseUnitTests()
    {
        _useCase = new(
            _refreshTokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _jwtServiceMock.Object,
            _refreshTokenServiceMock.Object,
            LoggerMock.Object
        );
    }

    private static User CreateUser() => new(
        Guid.NewGuid(),
        "test@example.com",
        "Test User",
        "hashed-password",
        UserRole.Admin,
        DateTime.Now,
        DateTime.Now
    );

    private RefreshToken CreateRefreshToken(Guid userId, DateTime? expiresAt = null, DateTime? revokedAt = null) => new(
        Guid.NewGuid(),
        userId,
        _fixture.Create<string>(),
        expiresAt ?? DateTime.Now.AddDays(7),
        DateTime.Now,
        DateTime.Now,
        revokedAt
    );

    private void SetupStoredRefreshToken(RefreshTokenInputContext context)
    {
        _refreshTokenServiceMock.Setup(x => x.Hash(context.Input.RefreshToken)).Returns(context.TokenHash);

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenHashAsync(context.TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(context.StoredRefreshToken);
    }

    private RefreshTokenInputContext CreateContext(RefreshToken? storedRefreshToken) => new(
        _fixture.Create<RefreshAccessTokenInput>(),
        _fixture.Create<string>(),
        storedRefreshToken
    );

    private GeneratedRefreshToken SetupTokenGeneration(User user, string accessToken)
    {
        var generatedRefreshToken = new GeneratedRefreshToken(
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            DateTime.Now.AddDays(7)
        );

        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtServiceMock.Setup(x => x.GenerateToken(user)).Returns(accessToken);

        _refreshTokenServiceMock.Setup(x => x.Generate()).Returns(generatedRefreshToken);

        return generatedRefreshToken;
    }

    private void VerifyNoTokenWasPersisted()
    {
        _refreshTokenRepositoryMock.Verify(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _refreshTokenRepositoryMock.Verify(x => x.Update(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact(DisplayName = "Handle >> Should Success >> When refresh token is active")]
    public async Task Handle_ShouldSuccess_WhenRefreshTokenIsActive()
    {
        // Arrange
        var user = CreateUser();
        var context = CreateContext(CreateRefreshToken(user.Id));
        var accessToken = _fixture.Create<string>();

        SetupStoredRefreshToken(context);
        var generatedRefreshToken = SetupTokenGeneration(user, accessToken);

        RefreshToken? addedRefreshToken = null;
        _refreshTokenRepositoryMock.Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((refreshToken, _) => addedRefreshToken = refreshToken);

        _refreshTokenRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(context.Input, CancellationToken.None);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().Be(new LoginResponse(accessToken, generatedRefreshToken.Token));

        addedRefreshToken.Should().NotBeNull();
        addedRefreshToken!.UserId.Should().Be(user.Id);
        addedRefreshToken.TokenHash.Should().Be(generatedRefreshToken.TokenHash);
        addedRefreshToken.ExpiresAt.Should().Be(generatedRefreshToken.ExpiresAt);
        addedRefreshToken.IsRevoked.Should().BeFalse();

        context.StoredRefreshToken!.IsRevoked.Should().BeTrue();
        context.StoredRefreshToken.ReplacedByTokenId.Should().Be(addedRefreshToken.Id);
        _refreshTokenRepositoryMock.Verify(x => x.Update(context.StoredRefreshToken), Times.Once);
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When refresh token does not exist")]
    public async Task Handle_ShouldFail_WhenRefreshTokenDoesNotExist()
    {
        // Arrange
        var context = CreateContext(null);
        SetupStoredRefreshToken(context);

        // Act
        var result = await _useCase.Handle(context.Input, CancellationToken.None);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Refresh token not found.");
        result.Result.Should().BeNull();
        VerifyNoTokenWasPersisted();
        VerifyLog(
            LogLevel.Warning,
            $"[{context.Input.CorrelationId}] Refresh token not found.",
            Times.Once()
        );
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When refresh token has already been revoked")]
    public async Task Handle_ShouldFail_WhenRefreshTokenHasAlreadyBeenRevoked()
    {
        // Arrange
        var storedRefreshToken = CreateRefreshToken(Guid.NewGuid(), revokedAt: DateTime.Now.AddMinutes(-5));
        var context = CreateContext(storedRefreshToken);
        SetupStoredRefreshToken(context);

        // Act
        var result = await _useCase.Handle(context.Input, CancellationToken.None);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Refresh token has already been revoked.");
        result.Result.Should().BeNull();
        VerifyNoTokenWasPersisted();
        VerifyLog(
            LogLevel.Warning,
            $"[{context.Input.CorrelationId}] Refresh token {storedRefreshToken.Id} has already been revoked.",
            Times.Once()
        );
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When refresh token has expired")]
    public async Task Handle_ShouldFail_WhenRefreshTokenHasExpired()
    {
        // Arrange
        var storedRefreshToken = CreateRefreshToken(Guid.NewGuid(), expiresAt: DateTime.Now.AddMinutes(-1));
        var context = CreateContext(storedRefreshToken);
        SetupStoredRefreshToken(context);

        // Act
        var result = await _useCase.Handle(context.Input, CancellationToken.None);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Refresh token has expired.");
        result.Result.Should().BeNull();
        VerifyNoTokenWasPersisted();
        VerifyLog(
            LogLevel.Warning,
            $"[{context.Input.CorrelationId}] Refresh token {storedRefreshToken.Id} has expired.",
            Times.Once()
        );
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When user of the refresh token does not exist")]
    public async Task Handle_ShouldFail_WhenUserOfTheRefreshTokenDoesNotExist()
    {
        // Arrange
        var storedRefreshToken = CreateRefreshToken(Guid.NewGuid());
        var context = CreateContext(storedRefreshToken);
        SetupStoredRefreshToken(context);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(storedRefreshToken.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _useCase.Handle(context.Input, CancellationToken.None);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("User not found.");
        result.Result.Should().BeNull();
        VerifyNoTokenWasPersisted();
        VerifyLog(
            LogLevel.Warning,
            $"[{context.Input.CorrelationId}] User {storedRefreshToken.UserId} of refresh token {storedRefreshToken.Id} not found.",
            Times.Once()
        );
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error rotating refresh token")]
    public async Task Handle_ShouldFail_WhenErrorRotatingRefreshToken()
    {
        // Arrange
        var user = CreateUser();
        var storedRefreshToken = CreateRefreshToken(user.Id);
        var context = CreateContext(storedRefreshToken);

        SetupStoredRefreshToken(context);
        SetupTokenGeneration(user, _fixture.Create<string>());

        _refreshTokenRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(context.Input, CancellationToken.None);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Error rotating refresh token.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{context.Input.CorrelationId}] Error rotating refresh token {storedRefreshToken.Id}.",
            Times.Once()
        );
    }

    private sealed record RefreshTokenInputContext(
        RefreshAccessTokenInput Input,
        string TokenHash,
        RefreshToken? StoredRefreshToken
    );
}
