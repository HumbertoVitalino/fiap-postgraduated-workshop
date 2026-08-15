using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.UpdateUser.Boundaries;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class UpdateUserUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateUserInput CreateUserInput() => new(
        Guid.NewGuid(),
        "Integration User",
        TestData.Email(),
        TestData.ShortString(16),
        UserRole.Admin
    );

    private async Task<UserResponse> CreateUserAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        var result = await createUseCase.Handle(CreateUserInput(), CancellationToken.None);
        return result.Result.Should().BeOfType<UserResponse>().Subject;
    }

    private async Task<Output> HandleAsync(UpdateUserInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateUserUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "UpdateUserUseCase >> Should persist changes >> When user exists")]
    public async Task Handle_ShouldPersistChanges_WhenUserExists()
    {
        // Arrange
        var created = await CreateUserAsync();
        var newEmail = TestData.Email();
        var input = new UpdateUserInput(Guid.NewGuid(), created.Id, "Updated Name", newEmail, UserRole.Mechanic);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var updated = result.Result.Should().BeOfType<UserResponse>().Subject;
        updated.Id.Should().Be(created.Id);
        updated.Name.Should().Be("Updated Name");
        updated.Email.Should().Be(newEmail);
        updated.Role.Should().Be(UserRole.Mechanic.ToString());

        using var scope = fixture.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var found = await userRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Updated Name");
        found.Email.Should().Be(newEmail);
        found.Role.Should().Be(UserRole.Mechanic);
    }

    [Fact(DisplayName = "UpdateUserUseCase >> Should fail >> When user does not exist")]
    public async Task Handle_ShouldFail_WhenUserDoesNotExist()
    {
        // Arrange
        var input = new UpdateUserInput(Guid.NewGuid(), Guid.NewGuid(), "Ghost User", TestData.Email(), UserRole.Admin);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find user");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "UpdateUserUseCase >> Should fail >> When new email already belongs to another user")]
    public async Task Handle_ShouldFail_WhenNewEmailAlreadyBelongsToAnotherUser()
    {
        // Arrange
        var other = await CreateUserAsync();
        var created = await CreateUserAsync();
        var input = new UpdateUserInput(Guid.NewGuid(), created.Id, created.Name, other.Email, UserRole.Admin);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle()
            .Which.Should().Be($"User with email {other.Email} already exists.");
        result.Result.Should().BeNull();
    }
}
