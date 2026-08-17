using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Services.DeleteService;
using Fiap.Workshop.Application.UseCases.Services.DeleteService.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class DeleteServiceUseCaseUnitTests : LoggerTestBase<DeleteServiceUseCase>
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly DeleteServiceUseCase _useCase;

    public DeleteServiceUseCaseUnitTests()
    {
        _useCase = new(
            _serviceRepositoryMock.Object,
            _serviceOrderRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private DeleteServiceInput CreateInput(Guid serviceId) => new(
        _fixture.Create<Guid>(),
        serviceId
    );

    private static Service CreateService(Guid id) => new(
        id,
        "SV-001",
        "Oil Change",
        "Oil change service",
        120m,
        30,
        true,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When service exists and has never been used")]
    public async Task Handle_ShouldSuccess_WhenServiceExistsAndHasNeverBeenUsed()
    {
        // Arrange
        var service = CreateService(_fixture.Create<Guid>());
        var input = CreateInput(service.Id);

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        _serviceOrderRepositoryMock.Setup(x => x.ExistsWithServiceIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _serviceRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeNull();

        _serviceRepositoryMock.Verify(x => x.Remove(service), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Succeed idempotently >> When service does not exist")]
    public async Task Handle_ShouldSucceedIdempotently_WhenServiceDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>());

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeNull();

        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find service by [{input.ServiceId}].",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.ExistsWithServiceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
        _serviceRepositoryMock.Verify(x => x.Remove(It.IsAny<Service>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service has been used in a service order")]
    public async Task Handle_ShouldFail_WhenServiceHasBeenUsedInServiceOrder()
    {
        // Arrange
        var service = CreateService(_fixture.Create<Guid>());
        var input = CreateInput(service.Id);

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        _serviceOrderRepositoryMock.Setup(x => x.ExistsWithServiceIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Service has been used in a service order and cannot be deleted.");
        result.Result.Should().BeNull();

        _serviceRepositoryMock.Verify(x => x.Remove(It.IsAny<Service>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error deleting service")]
    public async Task Handle_ShouldFail_WhenErrorDeletingService()
    {
        // Arrange
        var service = CreateService(_fixture.Create<Guid>());
        var input = CreateInput(service.Id);

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        _serviceOrderRepositoryMock.Setup(x => x.ExistsWithServiceIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _serviceRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error deleting service with id {input.ServiceId}.");
        result.Result.Should().BeNull();

        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error deleting service with id {input.ServiceId}.",
            Times.Once()
        );
    }
}
