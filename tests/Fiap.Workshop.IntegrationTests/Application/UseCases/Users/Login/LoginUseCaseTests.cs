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
    private const string ValidPassword = "ValidPass123";

    [Fact(DisplayName = "ExecuteAsync >> Should Return Token >> When User Exists")]
    public async Task ExecuteAsync_ShouldReturnToken_WhenUserExists()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        var loginUseCase = scope.ServiceProvider.GetRequiredService<ILoginUseCase>();
        var correlationId = Guid.NewGuid();

        var email = $"{correlationId:N}@example.com";
        await createUseCase.ExecuteAsync(new CreateUserInput(Guid.NewGuid(), "Alice", email, ValidPassword, UserRole.User));

        // Act
        var output = await loginUseCase.ExecuteAsync(new LoginInput(correlationId, email, ValidPassword));

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
        var output = await useCase.ExecuteAsync(new LoginInput(Guid.NewGuid(), "nobody@example.com", ValidPassword));

        // Assert
        output.IsValid.Should().BeFalse();
        output.ErrorMessages.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Error >> When Password Does Not Match")]
    public async Task ExecuteAsync_ShouldReturnError_WhenPasswordDoesNotMatch()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        var loginUseCase = scope.ServiceProvider.GetRequiredService<ILoginUseCase>();
        var correlationId = Guid.NewGuid();

        var email = $"{correlationId:N}@example.com";
        await createUseCase.ExecuteAsync(new CreateUserInput(correlationId, "Alice", email, ValidPassword, UserRole.User));

        // Act
        var output = await loginUseCase.ExecuteAsync(new LoginInput(correlationId, email, "WrongPass123"));

        // Assert
        output.IsValid.Should().BeFalse();
        output.ErrorMessages.Should().Contain(UserErrors.InvalidCredentials);
    }
}
