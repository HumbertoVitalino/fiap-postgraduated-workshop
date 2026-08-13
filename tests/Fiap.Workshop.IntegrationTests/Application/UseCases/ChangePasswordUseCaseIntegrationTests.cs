using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ChangePassword.Boundaries;
using Fiap.Workshop.Application.UseCases.CreateUser.Boundaries;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class ChangePasswordUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private const string CurrentPassword = "Current@Password1";

    private static CreateUserInput CreateUserInput() => new(
        Guid.NewGuid(),
        "Integration User",
        TestData.Email(),
        CurrentPassword,
        UserRole.Admin
    );

    private async Task<UserResponse> CreateUserAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        var result = await createUseCase.Handle(CreateUserInput(), CancellationToken.None);
        return result.Result.Should().BeOfType<UserResponse>().Subject;
    }

    private async Task<Output> HandleAsync(ChangePasswordInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IChangePasswordUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "ChangePasswordUseCase >> Should persist new password >> When current password is correct")]
    public async Task Handle_ShouldPersistNewPassword_WhenCurrentPasswordIsCorrect()
    {
        // Arrange
        var created = await CreateUserAsync();
        var newPassword = TestData.ShortString(16);
        var input = new ChangePasswordInput(Guid.NewGuid(), created.Id, CurrentPassword, newPassword);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeOfType<UserResponse>();

        using var scope = fixture.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        var found = await userRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
        passwordService.Verify(newPassword, found!.Password).Should().BeTrue();
        passwordService.Verify(CurrentPassword, found.Password).Should().BeFalse();
    }

    [Fact(DisplayName = "ChangePasswordUseCase >> Should fail >> When user does not exist")]
    public async Task Handle_ShouldFail_WhenUserDoesNotExist()
    {
        // Arrange
        var input = new ChangePasswordInput(Guid.NewGuid(), Guid.NewGuid(), CurrentPassword, TestData.ShortString(16));

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find user");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "ChangePasswordUseCase >> Should fail >> When current password is incorrect")]
    public async Task Handle_ShouldFail_WhenCurrentPasswordIsIncorrect()
    {
        // Arrange
        var created = await CreateUserAsync();
        var input = new ChangePasswordInput(Guid.NewGuid(), created.Id, "wrong-password", TestData.ShortString(16));

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Invalid current password");
        result.Result.Should().BeNull();
    }
}
