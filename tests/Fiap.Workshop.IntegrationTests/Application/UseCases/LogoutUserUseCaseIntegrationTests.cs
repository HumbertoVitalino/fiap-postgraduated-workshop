using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.LoginUser.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.LogoutUser.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.RefreshAccessToken.Boundaries;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class LogoutUserUseCaseIntegrationTests(DatabaseFixture fixture)
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
        var useCase = scope.ServiceProvider.GetRequiredService<ILogoutUserUseCase>();
        return await useCase.Handle(new LogoutUserInput(Guid.NewGuid(), refreshToken), CancellationToken.None);
    }

    private async Task<Output> RefreshAsync(string refreshToken)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IRefreshAccessTokenUseCase>();
        return await useCase.Handle(new RefreshAccessTokenInput(Guid.NewGuid(), refreshToken), CancellationToken.None);
    }

    [Fact(DisplayName = "LogoutUserUseCase >> Should revoke refresh token >> When refresh token is active")]
    public async Task Handle_ShouldRevokeRefreshToken_WhenRefreshTokenIsActive()
    {
        // Arrange
        var login = await LoginAsync();

        // Act
        var result = await HandleAsync(login.RefreshToken);

        // Assert
        result.IsValid.Should().BeTrue();

        var refreshResult = await RefreshAsync(login.RefreshToken);
        refreshResult.ErrorMessages.Should().ContainSingle().Which.Should().Be("Refresh token has already been revoked.");
    }

    [Fact(DisplayName = "LogoutUserUseCase >> Should succeed >> When logging out twice with the same refresh token")]
    public async Task Handle_ShouldSucceed_WhenLoggingOutTwiceWithTheSameRefreshToken()
    {
        // Arrange
        var login = await LoginAsync();
        await HandleAsync(login.RefreshToken);

        // Act
        var result = await HandleAsync(login.RefreshToken);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "LogoutUserUseCase >> Should succeed >> When refresh token does not exist")]
    public async Task Handle_ShouldSucceed_WhenRefreshTokenDoesNotExist()
    {
        // Act
        var result = await HandleAsync(Guid.NewGuid().ToString("N"));

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
    }
}
