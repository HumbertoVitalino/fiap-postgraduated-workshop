using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.LoginUser.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.RefreshAccessToken.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class RefreshAccessTokenUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private const string Password = "Integration@Pass123";

    private async Task<LoginResponse> LoginAsync()
    {
        var email = TestData.Email();

        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        await createUseCase.Handle(
            new CreateUserInput(Guid.NewGuid(), "Integration User", email, Password, UserRole.Admin),
            CancellationToken.None
        );

        var loginUseCase = scope.ServiceProvider.GetRequiredService<ILoginUserUseCase>();
        var result = await loginUseCase.Handle(new LoginUserInput(Guid.NewGuid(), email, Password), CancellationToken.None);

        return result.GetResult<LoginResponse>()!;
    }

    private async Task<Output> HandleAsync(string refreshToken)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRefreshAccessTokenUseCase>();
        return await useCase.Handle(new RefreshAccessTokenInput(Guid.NewGuid(), refreshToken), CancellationToken.None);
    }

    private async Task<RefreshToken?> GetStoredAsync(string refreshToken)
    {
        using var scope = fixture.Services.CreateScope();
        var refreshTokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
        var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
        return await refreshTokenRepository.GetByTokenHashAsync(refreshTokenService.Hash(refreshToken), CancellationToken.None);
    }

    [Fact(DisplayName = "RefreshAccessTokenUseCase >> Should rotate refresh token >> When refresh token is active")]
    public async Task Handle_ShouldRotateRefreshToken_WhenRefreshTokenIsActive()
    {
        // Arrange
        var login = await LoginAsync();

        // Act
        var result = await HandleAsync(login.RefreshToken);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<LoginResponse>().Subject;
        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.RefreshToken.Should().NotBe(login.RefreshToken);

        var previous = await GetStoredAsync(login.RefreshToken);
        var current = await GetStoredAsync(response.RefreshToken);

        current.Should().NotBeNull();
        current!.IsActive().Should().BeTrue();
        current.UserId.Should().Be(previous!.UserId);

        previous.IsRevoked.Should().BeTrue();
        previous.ReplacedByTokenId.Should().Be(current.Id);
    }

    [Fact(DisplayName = "RefreshAccessTokenUseCase >> Should fail >> When refresh token has already been rotated")]
    public async Task Handle_ShouldFail_WhenRefreshTokenHasAlreadyBeenRotated()
    {
        // Arrange
        var login = await LoginAsync();
        await HandleAsync(login.RefreshToken);

        // Act
        var result = await HandleAsync(login.RefreshToken);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Refresh token has already been revoked.");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "RefreshAccessTokenUseCase >> Should fail >> When refresh token does not exist")]
    public async Task Handle_ShouldFail_WhenRefreshTokenDoesNotExist()
    {
        // Act
        var result = await HandleAsync(Guid.NewGuid().ToString("N"));

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Refresh token not found.");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "RefreshAccessTokenUseCase >> Should fail >> When refresh token has expired")]
    public async Task Handle_ShouldFail_WhenRefreshTokenHasExpired()
    {
        // Arrange
        var login = await LoginAsync();
        var stored = await GetStoredAsync(login.RefreshToken);

        var expired = new RefreshToken(
            stored!.Id,
            stored.UserId,
            stored.TokenHash,
            DateTime.Now.AddMinutes(-1),
            stored.CreatedAt,
            DateTime.Now
        );

        using (var scope = fixture.Services.CreateScope())
        {
            var refreshTokenRepository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
            refreshTokenRepository.Update(expired);
            await refreshTokenRepository.UnitOfWork.CommitAsync(CancellationToken.None);
        }

        // Act
        var result = await HandleAsync(login.RefreshToken);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Refresh token has expired.");
        result.Result.Should().BeNull();
    }
}
