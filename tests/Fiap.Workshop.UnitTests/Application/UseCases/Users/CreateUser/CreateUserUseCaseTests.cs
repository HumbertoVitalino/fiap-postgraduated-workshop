using AutoFixture;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Users.CreateUser;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases.Users.CreateUser;

public sealed class CreateUserUseCaseTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Fixture _fixture = new();
    private readonly CreateUserUseCase _sut;

    public CreateUserUseCaseTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _repositoryMock.Setup(r => r.UnitOfWork).Returns(_unitOfWorkMock.Object);
        _sut = new CreateUserUseCase(_repositoryMock.Object, NullLogger<CreateUserUseCase>.Instance);
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Success Output With User Response >> When Input Is Valid")]
    public async Task ExecuteAsync_ValidInput_ReturnsSuccessOutputWithUserResponse()
    {
        // Arrange
        var input = _fixture.Build<CreateUserInput>()
            .With(x => x.Email, "test@hotmail.com")
            .Create();

        _repositoryMock
            .Setup(r => r.ExistsWithEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var output = await _sut.ExecuteAsync(input);

        // Assert
        output.IsValid.Should().BeTrue();
        output.Result.Should().BeOfType<UserResponse>();
        var response = (UserResponse)output.Result!;
        response.Name.Should().Be(input.Name);
        response.Email.Should().Be(input.Email);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Invalid Output With Email In Use Error >> When Email Is Already Registered")]
    public async Task ExecuteAsync_DuplicateEmail_ReturnsInvalidOutputWithEmailInUseError()
    {
        // Arrange
        var input = _fixture.Build<CreateUserInput>()
            .With(x => x.Email, "test@hotmail.com")
            .Create();

        _repositoryMock
            .Setup(r => r.ExistsWithEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var output = await _sut.ExecuteAsync(input);

        // Assert
        output.IsValid.Should().BeFalse();
        output.ErrorMessages.Should().Contain(UserErrors.EmailAlreadyInUse);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Invalid Output With Commit Error >> When Commit Fails")]
    public async Task ExecuteAsync_CommitFails_ReturnsInvalidOutput()
    {
        // Arrange
        var input = _fixture.Build<CreateUserInput>()
            .With(x => x.Email, "test@hotmail.com")
            .Create();

        _repositoryMock
            .Setup(r => r.ExistsWithEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var output = await _sut.ExecuteAsync(input);

        // Assert
        output.IsValid.Should().BeFalse();
        output.ErrorMessages.Should().NotBeEmpty();
    }
}
