using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.CreateUser.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class CreateUserUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateUserInput CreateInput(string? email = null) => new(
        Guid.NewGuid(),
        "Integration User",
        email ?? TestData.Email(),
        TestData.ShortString(16),
        UserRole.User
    );

    private async Task<Output> HandleAsync(CreateUserInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "CreateUserUseCase >> Should persist user >> When user is valid and does not exist")]
    public async Task Handle_ShouldPersistUser_WhenUserIsValidAndDoesNotExist()
    {
        // Arrange
        var input = CreateInput();

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var created = result.Result.Should().BeOfType<User>().Subject;
        created.Email.Should().Be(input.Email);
        created.Password.Should().NotBe(input.Password);

        using var scope = fixture.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var found = await userRepository.GetByEmailAsync(input.Email, CancellationToken.None);
        found.Should().NotBeNull();
    }

    [Fact(DisplayName = "CreateUserUseCase >> Should fail >> When user already exists")]
    public async Task Handle_ShouldFail_WhenUserAlreadyExists()
    {
        // Arrange
        var input = CreateInput();
        await HandleAsync(input);

        var duplicateInput = CreateInput(input.Email);

        // Act
        var result = await HandleAsync(duplicateInput);

        // Assert
        result.ErrorMessages.Should().ContainSingle()
            .Which.Should().Be($"User with email {duplicateInput.Email} already exists.");
        result.Result.Should().BeNull();
    }
}
