using AutoFixture;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.UseCases.Users.LoginUser;
using Fiap.Workshop.Application.UseCases.Users.LoginUser.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class LoginUserUseCaseUnitTests : LoggerTestBase<LoginUserUseCase>
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordService> _passwordServiceMock = new();
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Fixture _fixture = new();
    private readonly LoginUserUseCase _useCase;

    public LoginUserUseCaseUnitTests()
    {
        _useCase = new(
            _userRepositoryMock.Object,
            _passwordServiceMock.Object,
            _jwtServiceMock.Object,
            LoggerMock.Object
        );
    }

    private static User CreateUser(string email, string password) => new(
        Guid.NewGuid(),
        email,
        "Test User",
        password,
        UserRole.Admin,
        DateTime.UtcNow,
        DateTime.UtcNow
    );

    [Fact(DisplayName = "Handle >> Should Success >> When credentials are valid")]
    public async Task Handle_ShouldSuccess_WhenCredentialsAreValid()
    {
        // Arrange
        var input = _fixture.Build<LoginUserInput>()
            .With(x => x.Email, "test@example.com")
            .Create();

        var user = CreateUser(input.Email, "hashed-password");
        var token = _fixture.Create<string>();

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(input.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(x => x.Verify(input.Password, user.Password)).Returns(true);

        _jwtServiceMock.Setup(x => x.GenerateToken(user)).Returns(token);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().Be(token);
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When user does not exist")]
    public async Task Handle_ShouldFail_WhenUserDoesNotExist()
    {
        // Arrange
        var input = _fixture.Build<LoginUserInput>()
            .With(x => x.Email, "test@example.com")
            .Create();

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(input.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().NotBeEmpty();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("User not found.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] User not found with email: {input.Email.MaskEmail()}",
            Times.Once()
        );
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When password is invalid")]
    public async Task Handle_ShouldFail_WhenPasswordIsInvalid()
    {
        // Arrange
        var input = _fixture.Build<LoginUserInput>()
            .With(x => x.Email, "test@example.com")
            .Create();

        var user = CreateUser(input.Email, "hashed-password");

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(input.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(x => x.Verify(input.Password, user.Password)).Returns(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().NotBeEmpty();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Invalid password.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] Invalid password for user: {input.Email.MaskEmail()}",
            Times.Once()
        );
    }
}
