using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Vehicles.DeleteVehicle;
using Fiap.Workshop.Application.UseCases.Vehicles.DeleteVehicle.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class DeleteVehicleUseCaseUnitTests : LoggerTestBase<DeleteVehicleUseCase>
{
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock = new();
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly DeleteVehicleUseCase _useCase;

    public DeleteVehicleUseCaseUnitTests()
    {
        _useCase = new(
            _vehicleRepositoryMock.Object,
            _serviceOrderRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private DeleteVehicleInput CreateInput(Guid vehicleId) => new(
        _fixture.Create<Guid>(),
        vehicleId
    );

    private static Vehicle CreateVehicle(Guid id) => new(
        id,
        Guid.NewGuid(),
        "ABC1234",
        "Ford",
        "Ka",
        2020,
        2020,
        "Black",
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When vehicle exists and has no service orders")]
    public async Task Handle_ShouldSuccess_WhenVehicleExistsAndHasNoServiceOrders()
    {
        // Arrange
        var vehicle = CreateVehicle(_fixture.Create<Guid>());
        var input = CreateInput(vehicle.Id);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _serviceOrderRepositoryMock.Setup(x => x.ExistsWithVehicleIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _vehicleRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeNull();

        _vehicleRepositoryMock.Verify(x => x.Remove(vehicle), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Succeed idempotently >> When vehicle does not exist")]
    public async Task Handle_ShouldSucceedIdempotently_WhenVehicleDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>());

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeNull();

        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find vehicle by [{input.VehicleId}].",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.ExistsWithVehicleIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
        _vehicleRepositoryMock.Verify(x => x.Remove(It.IsAny<Vehicle>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When vehicle has service orders associated")]
    public async Task Handle_ShouldFail_WhenVehicleHasServiceOrdersAssociated()
    {
        // Arrange
        var vehicle = CreateVehicle(_fixture.Create<Guid>());
        var input = CreateInput(vehicle.Id);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _serviceOrderRepositoryMock.Setup(x => x.ExistsWithVehicleIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Vehicle has service orders associated and cannot be deleted.");
        result.Result.Should().BeNull();

        _vehicleRepositoryMock.Verify(x => x.Remove(It.IsAny<Vehicle>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error deleting vehicle")]
    public async Task Handle_ShouldFail_WhenErrorDeletingVehicle()
    {
        // Arrange
        var vehicle = CreateVehicle(_fixture.Create<Guid>());
        var input = CreateInput(vehicle.Id);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _serviceOrderRepositoryMock.Setup(x => x.ExistsWithVehicleIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _vehicleRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error deleting vehicle with id {input.VehicleId}.");
        result.Result.Should().BeNull();

        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error deleting vehicle with id {input.VehicleId}.",
            Times.Once()
        );
    }
}
