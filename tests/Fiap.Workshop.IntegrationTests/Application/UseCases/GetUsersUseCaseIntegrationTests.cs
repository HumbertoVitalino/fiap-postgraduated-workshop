using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class GetUsersUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<Output> HandleAsync(Guid input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetUsersUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "GetUsersUseCase >> Should return user >> When users exists")]
    public async Task GetUsersUseCase_ShouldReturnUsers_WhenUsersExists()
    {
        // Arrange
        var created = await CreateUserAsync();

        // Act
        var result = await HandleAsync(Guid.NewGuid());

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var users = result.Result.Should().BeAssignableTo<IEnumerable<UserResponse>>().Subject;
        users.Should().ContainSingle(u => u.Id == created.Id)
            .Which.Should().BeEquivalentTo(created);
    }
}
