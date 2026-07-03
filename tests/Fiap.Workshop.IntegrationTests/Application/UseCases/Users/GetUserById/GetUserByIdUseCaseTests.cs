using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.GetUserById.Boundaries;
using Fiap.Workshop.Domain.Users;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases.Users.GetUserById;

[Collection("Integration")]
public sealed class GetUserByIdUseCaseTests(DatabaseFixture fixture)
{
    [Fact(DisplayName = "ExecuteAsync >> Should Return User >> When User Exists")]
    public async Task ExecuteAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        var getUseCase = scope.ServiceProvider.GetRequiredService<IGetUserByIdUseCase>();

        var createOutput = await createUseCase.ExecuteAsync(
            new CreateUserInput(Guid.NewGuid(), "Jane Doe", $"{Guid.NewGuid():N}@example.com", UserRole.User));

        var created = (createOutput.Result as UserResponse)!;

        // Act
        var output = await getUseCase.ExecuteAsync(new GetUserByIdInput(created.Id));

        // Assert
        output.IsValid.Should().BeTrue();
        var response = output.Result as UserResponse;
        response.Should().NotBeNull();
        response!.Id.Should().Be(created.Id);
        response.Name.Should().Be(created.Name);
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Error >> When User Does Not Exist")]
    public async Task ExecuteAsync_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetUserByIdUseCase>();

        // Act
        var output = await useCase.ExecuteAsync(new GetUserByIdInput(Guid.NewGuid()));

        // Assert
        output.IsValid.Should().BeFalse();
        output.ErrorMessages.Should().NotBeEmpty();
    }
}
