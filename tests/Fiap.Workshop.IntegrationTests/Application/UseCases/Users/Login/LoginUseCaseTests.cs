using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.Login.Boundaries;
using Fiap.Workshop.Domain.Users;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases.Users.Login;

[Collection("Integration")]
public sealed class LoginUseCaseTests(DatabaseFixture fixture)
{
    [Fact(DisplayName = "ExecuteAsync >> Should Return Token >> When User Exists")]
    public async Task ExecuteAsync_ShouldReturnToken_WhenUserExists()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        var loginUseCase = scope.ServiceProvider.GetRequiredService<ILoginUseCase>();

        var email = $"{Guid.NewGuid():N}@example.com";
        await createUseCase.ExecuteAsync(new CreateUserInput(Guid.NewGuid(), "Alice", email, UserRole.User));

        // Act
        var output = await loginUseCase.ExecuteAsync(new LoginInput(email));

        // Assert
        output.IsValid.Should().BeTrue();
        var response = output.Result as LoginResponse;
        response.Should().NotBeNull();
        response!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Error >> When Email Does Not Match")]
    public async Task ExecuteAsync_ShouldReturnError_WhenEmailDoesNotMatch()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ILoginUseCase>();

        // Act
        var output = await useCase.ExecuteAsync(new LoginInput("nobody@example.com"));

        // Assert
        output.IsValid.Should().BeFalse();
        output.ErrorMessages.Should().NotBeEmpty();
    }
}
