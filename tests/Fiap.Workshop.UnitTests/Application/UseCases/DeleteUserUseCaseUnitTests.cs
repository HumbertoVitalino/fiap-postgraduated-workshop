using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.DeleteUser;
using Fiap.Workshop.Application.UseCases.DeleteUser.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class DeleteUserUseCaseUnitTests : LoggerTestBase<DeleteUserUseCase>
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly DeleteUserUseCase _useCase;

    public DeleteUserUseCaseUnitTests()
    {
        _useCase = new(
            _userRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private DeleteUserInput CreateInput(Guid userId) => new(
        _fixture.Create<Guid>(),
        userId
    );

    private static User CreateUser(Guid id) => new(
        id,
        "john.doe@example.com",
        "John Doe",
        "hashed-password",
        UserRole.Admin,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When user exists")]
    public async Task Handle_ShouldSuccess_WhenUserExists()
    {
        // Arrange
        var user = CreateUser(_fixture.Create<Guid>());
        var input = CreateInput(user.Id);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(input.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeNull();

        _userRepositoryMock.Verify(x => x.Remove(user), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Succeed idempotently >> When user does not exist")]
    public async Task Handle_ShouldSucceedIdempotently_WhenUserDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>());

        _userRepositoryMock.Setup(x => x.GetByIdAsync(input.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeNull();

        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find user by [{input.UserId}].",
            Times.Once()
        );
        _userRepositoryMock.Verify(x => x.Remove(It.IsAny<User>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error deleting user")]
    public async Task Handle_ShouldFail_WhenErrorDeletingUser()
    {
        // Arrange
        var user = CreateUser(_fixture.Create<Guid>());
        var input = CreateInput(user.Id);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(input.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error deleting user with id {input.UserId}.");
        result.Result.Should().BeNull();

        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error deleting user with id {input.UserId}.",
            Times.Once()
        );
    }
}
