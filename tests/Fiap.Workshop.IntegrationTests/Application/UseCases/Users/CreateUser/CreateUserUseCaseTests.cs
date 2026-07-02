using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases.Users.CreateUser;

[Collection("Integration")]
public sealed class CreateUserUseCaseTests(DatabaseFixture fixture)
{
    [Fact(DisplayName = "ExecuteAsync >> Should Create User >> When Input Is Valid")]
    public async Task ExecuteAsync_ShouldCreateUser_WhenInputIsValid()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        var input = new CreateUserInput("John Doe", $"{Guid.NewGuid():N}@example.com");

        // Act
        var output = await useCase.ExecuteAsync(input);

        // Assert
        output.IsValid.Should().BeTrue();
        var response = output.Result as UserResponse;
        response.Should().NotBeNull();
        response!.Email.Should().Be(input.Email.ToLowerInvariant());
        response.Name.Should().Be(input.Name);
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Error >> When Email Already Exists")]
    public async Task ExecuteAsync_ShouldReturnError_WhenEmailAlreadyExists()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        var email = $"{Guid.NewGuid():N}@example.com";

        await useCase.ExecuteAsync(new CreateUserInput("First User", email));

        // Act
        var output = await useCase.ExecuteAsync(new CreateUserInput("Second User", email));

        // Assert
        output.IsValid.Should().BeFalse();
        output.ErrorMessages.Should().NotBeEmpty();
    }
}
