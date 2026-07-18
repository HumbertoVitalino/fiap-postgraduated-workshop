using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.GetUserById.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.UpdateEmail.Boundaries;
using Fiap.Workshop.Domain.Users;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases.Users.UpdateEmail;

[Collection("Integration")]
public sealed class UpdateEmailUseCaseTests(DatabaseFixture fixture)
{
    [Fact(DisplayName = "ExecuteAsync >> Should Update Email And Persist Change >> When Input Is Valid")]
    public async Task ExecuteAsync_ShouldUpdateEmailAndPersistChange_WhenInputIsValid()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        var updateUseCase = scope.ServiceProvider.GetRequiredService<IUpdateEmailUseCase>();
        var getUseCase = scope.ServiceProvider.GetRequiredService<IGetUserByIdUseCase>();

        var createOutput = await createUseCase.ExecuteAsync(
            new CreateUserInput(Guid.NewGuid(), "Jane Doe", $"{Guid.NewGuid():N}@example.com", "ValidPass123", UserRole.User));
        var created = (createOutput.Result as UserResponse)!;
        var newEmail = $"{Guid.NewGuid():N}@example.com";

        // Act
        var updateOutput = await updateUseCase.ExecuteAsync(new UpdateEmailInput(Guid.NewGuid(), created.Id, newEmail));

        // Assert
        updateOutput.IsValid.Should().BeTrue();
        (updateOutput.Result as UserResponse)!.Email.Should().Be(newEmail);

        // Reload from the database through a separate use case call to prove the change was actually persisted,
        // not just held in the in-memory domain object returned by UpdateEmailUseCase.
        var reloadOutput = await getUseCase.ExecuteAsync(new GetUserByIdInput(created.Id, Guid.NewGuid()));

        reloadOutput.IsValid.Should().BeTrue();
        (reloadOutput.Result as UserResponse)!.Email.Should().Be(newEmail);
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Error >> When User Does Not Exist")]
    public async Task ExecuteAsync_ShouldReturnError_WhenUserDoesNotExist()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var updateUseCase = scope.ServiceProvider.GetRequiredService<IUpdateEmailUseCase>();

        // Act
        var output = await updateUseCase.ExecuteAsync(
            new UpdateEmailInput(Guid.NewGuid(), Guid.NewGuid(), $"{Guid.NewGuid():N}@example.com"));

        // Assert
        output.IsValid.Should().BeFalse();
        output.ErrorMessages.Should().Contain(UserErrors.NotFound);
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Error >> When Email Already Belongs To Another User")]
    public async Task ExecuteAsync_ShouldReturnError_WhenEmailAlreadyBelongsToAnotherUser()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        var updateUseCase = scope.ServiceProvider.GetRequiredService<IUpdateEmailUseCase>();

        var existingEmail = $"{Guid.NewGuid():N}@example.com";
        await createUseCase.ExecuteAsync(
            new CreateUserInput(Guid.NewGuid(), "First User", existingEmail, "ValidPass123", UserRole.User));

        var secondCreateOutput = await createUseCase.ExecuteAsync(
            new CreateUserInput(Guid.NewGuid(), "Second User", $"{Guid.NewGuid():N}@example.com", "ValidPass123", UserRole.User));
        var second = (secondCreateOutput.Result as UserResponse)!;

        // Act
        var output = await updateUseCase.ExecuteAsync(new UpdateEmailInput(Guid.NewGuid(), second.Id, existingEmail));

        // Assert
        output.IsValid.Should().BeFalse();
        output.ErrorMessages.Should().Contain(UserErrors.EmailAlreadyInUse);
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Success Without Error >> When New Email Is Same As Current")]
    public async Task ExecuteAsync_ShouldReturnSuccessWithoutError_WhenNewEmailIsSameAsCurrent()
    {
        // Arrange
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        var updateUseCase = scope.ServiceProvider.GetRequiredService<IUpdateEmailUseCase>();

        var email = $"{Guid.NewGuid():N}@example.com";
        var createOutput = await createUseCase.ExecuteAsync(
            new CreateUserInput(Guid.NewGuid(), "Jane Doe", email, "ValidPass123", UserRole.User));
        var created = (createOutput.Result as UserResponse)!;

        // Act — same email, just re-cased and padded, to prove normalization + the no-op path both work end-to-end.
        var output = await updateUseCase.ExecuteAsync(
            new UpdateEmailInput(Guid.NewGuid(), created.Id, $"  {email.ToUpperInvariant()}  "));

        // Assert
        output.IsValid.Should().BeTrue();
        (output.Result as UserResponse)!.Email.Should().Be(email);
    }
}
