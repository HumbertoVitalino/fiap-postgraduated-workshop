using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ProcessBudgetDecision;
using Fiap.Workshop.Application.UseCases.Users.DeleteUser.Boundaries;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class DeleteUserUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<Output> HandleAsync(DeleteUserInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IDeleteUserUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "DeleteUserUseCase >> Should remove user >> When user exists")]
    public async Task Handle_ShouldRemoveUser_WhenUserExists()
    {
        // Arrange
        var created = await CreateUserAsync();
        var input = new DeleteUserInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();

        using var scope = fixture.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var found = await userRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().BeNull();
    }

    [Fact(DisplayName = "DeleteUserUseCase >> Should fail and keep user >> When user is the system user")]
    public async Task Handle_ShouldFailAndKeepUser_WhenUserIsSystemUser()
    {
        // Arrange (usuário de sistema semeado pelo db/init.sql)
        var input = new DeleteUserInput(Guid.NewGuid(), ProcessBudgetDecisionUseCase.SystemUserId);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("System users cannot be deleted.");

        using var scope = fixture.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var found = await userRepository.GetByIdAsync(ProcessBudgetDecisionUseCase.SystemUserId, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Role.Should().Be(UserRole.System);
    }

    [Fact(DisplayName = "DeleteUserUseCase >> Should succeed idempotently >> When user does not exist")]
    public async Task Handle_ShouldSucceedIdempotently_WhenUserDoesNotExist()
    {
        // Arrange
        var input = new DeleteUserInput(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
    }
}
