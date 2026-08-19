using AutoFixture;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.UseCases.Users.ChangePassword;
using Fiap.Workshop.Application.UseCases.Users.ChangePassword.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class ChangePasswordUseCaseUnitTests : LoggerTestBase<ChangePasswordUseCase>
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordService> _passwordServiceMock = new();
    private readonly Fixture _fixture = new();
    private readonly ChangePasswordUseCase _useCase;

    public ChangePasswordUseCaseUnitTests()
    {
        _useCase = new(
            _userRepositoryMock.Object,
            _passwordServiceMock.Object,
            LoggerMock.Object
        );
    }

    private ChangePasswordInput CreateInput(Guid userId) => new(
        _fixture.Create<Guid>(),
        userId,
        "current-password",
        "new-password"
    );

    private static User CreateUser(Guid id) => new(
        id,
        "jane.doe@example.com",
        "Jane Doe",
        "hashed-current-password",
        UserRole.Admin,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When current password is valid")]
    public async Task Handle_ShouldSuccess_WhenCurrentPasswordIsValid()
    {
        // Arrange
        var user = CreateUser(_fixture.Create<Guid>());
        var input = CreateInput(user.Id);
        var newPasswordHash = _fixture.Create<string>();

        _userRepositoryMock.Setup(x => x.GetByIdAsync(input.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(x => x.Verify(input.CurrentPassword, user.Password))
            .Returns(true);

        _passwordServiceMock.Setup(x => x.Hash(input.NewPassword))
            .Returns(newPasswordHash);

        _userRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeOfType<UserResponse>();

        user.Password.Should().Be(newPasswordHash);
        _userRepositoryMock.Verify(x => x.Update(user), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When user does not exist")]
    public async Task Handle_ShouldFail_WhenUserDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>());

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

    [Fact(DisplayName = "Handle >> Should Fail >> When current password is invalid")]
    public async Task Handle_ShouldFail_WhenCurrentPasswordIsInvalid()
    {
        // Arrange
        var user = CreateUser(_fixture.Create<Guid>());
        var input = CreateInput(user.Id);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(input.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(x => x.Verify(input.CurrentPassword, user.Password))
            .Returns(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Invalid current password");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Invalid current password for user [{input.UserId}].",
            Times.Once()
        );
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error saving new password")]
    public async Task Handle_ShouldFail_WhenErrorSavingNewPassword()
    {
        // Arrange
        var user = CreateUser(_fixture.Create<Guid>());
        var input = CreateInput(user.Id);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(input.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(x => x.Verify(input.CurrentPassword, user.Password))
            .Returns(true);

        _passwordServiceMock.Setup(x => x.Hash(input.NewPassword))
            .Returns(_fixture.Create<string>());

        _userRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error changing password for user with id {input.UserId}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error changing password for user with id {input.UserId}.",
            Times.Once()
        );
    }
}
