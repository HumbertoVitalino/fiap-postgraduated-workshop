using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.GetUsers;
using Fiap.Workshop.Application.UseCases.GetUsers.Mapper;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetUsersUnitTests : LoggerTestBase<GetUsersUseCase>
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly GetUsersUseCase _useCase;

    public GetUsersUnitTests()
    {
        _useCase = new(
            _userRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    [Fact(DisplayName = "Handle >> Should Success >> When users exists")]
    public async Task Handle_ShouldSuccess_WhenUsersExists()
    {
        // Arrange
        var correlation = Guid.NewGuid();
        var users = _fixture.CreateMany<User>();

        _userRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync(users);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeEquivalentTo(users.MapToDto());
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "Handle >> Should Success and log >> When users not exists")]
    public async Task Handle_ShouldSuccessAndLog_WhenUsersNotExists()
    {
        // Arrange
        var correlation = Guid.NewGuid();

        _userRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync([]);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().Be(Array.Empty<User>());
        result.ErrorMessages.Should().BeEmpty();
        VerifyLog(
            LogLevel.Warning,
            $"[{correlation}] | Unable to find users",
            Times.Once()
        );
    }
}
