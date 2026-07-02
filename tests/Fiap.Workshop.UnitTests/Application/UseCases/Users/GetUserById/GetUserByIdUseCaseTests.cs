using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Users.GetUserById;
using Fiap.Workshop.Application.UseCases.Users.GetUserById.Boundaries;
using Fiap.Workshop.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases.Users.GetUserById;

public sealed class GetUserByIdUseCaseTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly GetUserByIdUseCase _sut;

    public GetUserByIdUseCaseTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _sut = new GetUserByIdUseCase(_repositoryMock.Object, NullLogger<GetUserByIdUseCase>.Instance);
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Success Output With User Response >> When User Exists")]
    public async Task ExecuteAsync_ExistingUser_ReturnsSuccessOutputWithUserResponse()
    {
        // Arrange
        var user = User.Create("john@example.com", "John Doe");
        var input = new GetUserByIdInput(user.Id);
        _repositoryMock
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var output = await _sut.ExecuteAsync(input);

        // Assert
        output.IsValid.Should().BeTrue();
        output.Result.Should().BeOfType<UserResponse>();
        var response = (UserResponse)output.Result!;
        response.Id.Should().Be(user.Id);
        response.Name.Should().Be("John Doe");
        response.Email.Should().Be("john@example.com");
    }

    [Fact(DisplayName = "ExecuteAsync >> Should Return Invalid Output With Not Found Error >> When User Does Not Exist")]
    public async Task ExecuteAsync_NonExistingUser_ReturnsInvalidOutputWithNotFoundError()
    {
        // Arrange
        var input = new GetUserByIdInput(Guid.NewGuid());
        _repositoryMock
            .Setup(r => r.GetByIdAsync(input.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<User?>(null));

        // Act
        var output = await _sut.ExecuteAsync(input);

        // Assert
        output.IsValid.Should().BeFalse();
        output.ErrorMessages.Should().Contain(UserErrors.NotFound);
    }
}
