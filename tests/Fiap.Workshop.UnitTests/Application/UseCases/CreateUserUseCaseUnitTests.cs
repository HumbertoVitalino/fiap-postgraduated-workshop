using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.UseCases.Users.CreateUser;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class CreateUserUseCaseUnitTests : LoggerTestBase<CreateUserUseCase>
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordService> _passwordServiceMock = new();
    private readonly Fixture _fixture = new();
    private CreateUserUseCase _useCase;

    public CreateUserUseCaseUnitTests()
    {
        _useCase = new(
            _userRepositoryMock.Object,
            _passwordServiceMock.Object,
            LoggerMock.Object
        );
    }

    [Fact(DisplayName = "Handle >> Should Success >> When user is valid and does not exist")]
    public async Task Handle_ShouldSuccess_WhenUserIsValidAndDoesNotExist()
    {
        // Arrange
        var input = _fixture.Build<CreateUserInput>()
            .With(x => x.Email, "test@example.com")
            .Create();

        var hashedPassword = _fixture.Create<string>();

        _userRepositoryMock.Setup(x => x.ExistsWithEmailAsync(input.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordServiceMock.Setup(x => x.Hash(input.Password)).Returns(hashedPassword);

        _userRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().NotBeNull();
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When user already exists")]
    public async Task Handle_ShouldFail_WhenUserAlreadyExists()
    {
        // Arrange
        var input = _fixture.Build<CreateUserInput>()
            .With(x => x.Email, "test@example.com")
            .Create();

        _userRepositoryMock.Setup(x => x.ExistsWithEmailAsync(input.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().NotBeEmpty();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"User with email {input.Email} already exists.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | User with email {input.Email} already exists.",
            Times.Once()
        );
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error saving user")]
    public async Task Handle_ShouldFail_WhenErrorSavingUser()
    {
        // Arrange
        var input = _fixture.Build<CreateUserInput>()
            .With(x => x.Email, "test@example.com")
            .Create();

        var hashedPassword = _fixture.Create<string>();

        _userRepositoryMock.Setup(x => x.ExistsWithEmailAsync(input.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordServiceMock.Setup(x => x.Hash(input.Password)).Returns(hashedPassword);

        _userRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().NotBeEmpty();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error saving user with email {input.Email}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error saving user with email {input.Email}.",
            Times.Once()
        );
    }
}
