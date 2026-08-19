using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Services.CreateService;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class CreateServiceUseCaseUnitTests : LoggerTestBase<CreateServiceUseCase>
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly CreateServiceUseCase _useCase;

    public CreateServiceUseCaseUnitTests()
    {
        _useCase = new(
            _serviceRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private CreateServiceInput CreateInput() => new(
        _fixture.Create<Guid>(),
        _fixture.Create<string>(),
        "Oil Change",
        "Engine oil and filter replacement",
        150.00m,
        60
    );

    [Fact(DisplayName = "Handle >> Should Success >> When code is not in use")]
    public async Task Handle_ShouldSuccess_WhenCodeIsNotInUse()
    {
        // Arrange
        var input = CreateInput();

        _serviceRepositoryMock.Setup(x => x.ExistsWithCodeAsync(input.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _serviceRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().NotBeNull();

        _serviceRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When code already exists")]
    public async Task Handle_ShouldFail_WhenCodeAlreadyExists()
    {
        // Arrange
        var input = CreateInput();

        _serviceRepositoryMock.Setup(x => x.ExistsWithCodeAsync(input.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Service with code {input.Code} already exists.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Service with code {input.Code} already exists.",
            Times.Once()
        );
        _serviceRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Service>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error saving service")]
    public async Task Handle_ShouldFail_WhenErrorSavingService()
    {
        // Arrange
        var input = CreateInput();

        _serviceRepositoryMock.Setup(x => x.ExistsWithCodeAsync(input.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _serviceRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error saving service with code {input.Code}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error saving service with code {input.Code}.",
            Times.Once()
        );
    }
}
