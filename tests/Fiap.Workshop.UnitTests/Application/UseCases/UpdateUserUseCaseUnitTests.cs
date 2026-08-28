using AutoFixture;
using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Users.UpdateUser;
using Fiap.Workshop.Application.UseCases.Users.UpdateUser.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class UpdateUserUseCaseUnitTests : LoggerTestBase<UpdateUserUseCase>
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly UpdateUserUseCase _useCase;

    public UpdateUserUseCaseUnitTests()
    {
        _useCase = new(
            _userRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private UpdateUserInput CreateInput(Guid userId, string email) => new(
        _fixture.Create<Guid>(),
        userId,
        "Jane Doe",
        email,
        UserRole.Attendant
    );

    private static User CreateUser(Guid id, string email) => new(
        id,
        email,
        "John Doe",
        "hashed-password",
        UserRole.Admin,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When email does not change")]
    public async Task Handle_ShouldSuccess_WhenEmailDoesNotChange()
    {
        // Arrange
        var user = CreateUser(_fixture.Create<Guid>(), "same@example.com");
        var input = CreateInput(user.Id, "same@example.com");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(input.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<UserResponse>().Which;
        response.Id.Should().Be(user.Id);
        response.Name.Should().Be(input.Name);
        response.Email.Should().Be(input.Email);
        response.Role.Should().Be(input.Role.ToString());

        _userRepositoryMock.Verify(x => x.ExistsWithEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
        _userRepositoryMock.Verify(x => x.Update(user), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Success >> When email changes and is not in use")]
    public async Task Handle_ShouldSuccess_WhenEmailChangesAndIsNotInUse()
    {
        // Arrange
        var user = CreateUser(_fixture.Create<Guid>(), "old@example.com");
        var input = CreateInput(user.Id, "new@example.com");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(input.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.ExistsWithEmailAsync(input.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<UserResponse>().Which;
        response.Email.Should().Be(input.Email);
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When user does not exist")]
    public async Task Handle_ShouldFail_WhenUserDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>(), "test@example.com");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(input.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find user");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find user by [{input.UserId}].",
            Times.Once()
        );
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When new email already belongs to another user")]
    public async Task Handle_ShouldFail_WhenNewEmailAlreadyBelongsToAnotherUser()
    {
        // Arrange
        var user = CreateUser(_fixture.Create<Guid>(), "old@example.com");
        var input = CreateInput(user.Id, "taken@example.com");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(input.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.ExistsWithEmailAsync(input.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"User with email {input.Email} already exists.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | User with email {input.Email.MaskEmail()} already exists.",
            Times.Once()
        );
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error updating user")]
    public async Task Handle_ShouldFail_WhenErrorUpdatingUser()
    {
        // Arrange
        var user = CreateUser(_fixture.Create<Guid>(), "same@example.com");
        var input = CreateInput(user.Id, "same@example.com");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(input.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error updating user with id {input.UserId}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error updating user with id {input.UserId}.",
            Times.Once()
        );
    }
}
