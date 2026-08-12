using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.CreateUser.Boundaries;
using Fiap.Workshop.Application.UseCases.LoginUser.Boundaries;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class LoginUserUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private const string Password = "Integration@Pass123";

    private static CreateUserInput CreateUserInput(string? email = null) => new(
        Guid.NewGuid(),
        "Integration User",
        email ?? TestData.Email(),
        Password,
        UserRole.Admin
    );

    private async Task<string> SeedUserAsync(string email)
    {
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateUserUseCase>();
        await createUseCase.Handle(CreateUserInput(email), CancellationToken.None);
        return email;
    }

    private async Task<Output> HandleAsync(LoginUserInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ILoginUserUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "LoginUserUseCase >> Should return token >> When credentials are valid")]
    public async Task Handle_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var email = await SeedUserAsync(TestData.Email());
        var input = new LoginUserInput(Guid.NewGuid(), email, Password);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var token = result.Result.Should().BeOfType<string>().Subject;
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "LoginUserUseCase >> Should fail >> When user does not exist")]
    public async Task Handle_ShouldFail_WhenUserDoesNotExist()
    {
        // Arrange
        var input = new LoginUserInput(Guid.NewGuid(), TestData.Email(), Password);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("User not found.");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "LoginUserUseCase >> Should fail >> When password is invalid")]
    public async Task Handle_ShouldFail_WhenPasswordIsInvalid()
    {
        // Arrange
        var email = await SeedUserAsync(TestData.Email());
        var input = new LoginUserInput(Guid.NewGuid(), email, "WrongPassword123");

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Invalid password.");
        result.Result.Should().BeNull();
    }
}
