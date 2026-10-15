using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.UseCases.Users.LogoutUser;
using Fiap.Workshop.Application.UseCases.Users.LogoutUser.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class LogoutUserUseCaseUnitTests : LoggerTestBase<LogoutUserUseCase>
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Fixture _fixture = new();
    private readonly LogoutUserUseCase _useCase;

    public LogoutUserUseCaseUnitTests()
    {
        _useCase = new(
            _refreshTokenRepositoryMock.Object,
            _refreshTokenServiceMock.Object,
            LoggerMock.Object
        );
    }

    private RefreshToken CreateRefreshToken(DateTime? revokedAt = null) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        _fixture.Create<string>(),
        DateTime.Now.AddDays(7),
        DateTime.Now,
        DateTime.Now,
        revokedAt
    );

    private LogoutUserInput SetupStoredRefreshToken(RefreshToken? storedRefreshToken)
    {
        var input = _fixture.Create<LogoutUserInput>();
        var tokenHash = _fixture.Create<string>();

        _refreshTokenServiceMock.Setup(x => x.Hash(input.RefreshToken)).Returns(tokenHash);

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedRefreshToken);

        return input;
    }

    [Fact(DisplayName = "Handle >> Should Success >> When refresh token is active")]
    public async Task Handle_ShouldSuccess_WhenRefreshTokenIsActive()
    {
        // Arrange
        var storedRefreshToken = CreateRefreshToken();
        var input = SetupStoredRefreshToken(storedRefreshToken);

        _refreshTokenRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        storedRefreshToken.IsRevoked.Should().BeTrue();
        storedRefreshToken.ReplacedByTokenId.Should().BeNull();
        _refreshTokenRepositoryMock.Verify(x => x.Update(storedRefreshToken), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Success without changes >> When refresh token does not exist")]
    public async Task Handle_ShouldSuccessWithoutChanges_WhenRefreshTokenDoesNotExist()
    {
        // Arrange
        var input = SetupStoredRefreshToken(null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        _refreshTokenRepositoryMock.Verify(x => x.Update(It.IsAny<RefreshToken>()), Times.Never());
        _refreshTokenRepositoryMock.Verify(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()), Times.Never());
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] Refresh token not found.",
            Times.Once()
        );
    }

    [Fact(DisplayName = "Handle >> Should Success without changes >> When refresh token has already been revoked")]
    public async Task Handle_ShouldSuccessWithoutChanges_WhenRefreshTokenHasAlreadyBeenRevoked()
    {
        // Arrange
        var revokedAt = DateTime.Now.AddMinutes(-5);
        var storedRefreshToken = CreateRefreshToken(revokedAt);
        var input = SetupStoredRefreshToken(storedRefreshToken);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        storedRefreshToken.RevokedAt.Should().Be(revokedAt);
        _refreshTokenRepositoryMock.Verify(x => x.Update(It.IsAny<RefreshToken>()), Times.Never());
        _refreshTokenRepositoryMock.Verify(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error revoking refresh token")]
    public async Task Handle_ShouldFail_WhenErrorRevokingRefreshToken()
    {
        // Arrange
        var storedRefreshToken = CreateRefreshToken();
        var input = SetupStoredRefreshToken(storedRefreshToken);

        _refreshTokenRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Error revoking refresh token.");
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] Error revoking refresh token {storedRefreshToken.Id}.",
            Times.Once()
        );
    }
}
