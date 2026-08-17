using AutoFixture;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Vehicles.UpdateVehicle;
using Fiap.Workshop.Application.UseCases.Vehicles.UpdateVehicle.Boundaries;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class UpdateVehicleUseCaseUnitTests : LoggerTestBase<UpdateVehicleUseCase>
{
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly UpdateVehicleUseCase _useCase;

    public UpdateVehicleUseCaseUnitTests()
    {
        _useCase = new(
            _vehicleRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private UpdateVehicleInput CreateInput(Guid vehicleId, int modelYear) => new(
        _fixture.Create<Guid>(),
        vehicleId,
        "Toyota",
        "Corolla",
        "White",
        modelYear
    );

    private static Vehicle CreateVehicle(Guid id, int manufactureYear) => new(
        id,
        Guid.NewGuid(),
        "ABC1234",
        "Ford",
        "Ka",
        manufactureYear,
        manufactureYear,
        "Black",
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When vehicle exists")]
    public async Task Handle_ShouldSuccess_WhenVehicleExists()
    {
        // Arrange
        var vehicle = CreateVehicle(_fixture.Create<Guid>(), 2020);
        var input = CreateInput(vehicle.Id, 2021);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _vehicleRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<VehicleResponse>().Which;
        response.Id.Should().Be(vehicle.Id);
        response.Brand.Should().Be("Toyota");
        response.ModelYear.Should().Be(2021);

        _vehicleRepositoryMock.Verify(x => x.Update(vehicle), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When vehicle does not exist")]
    public async Task Handle_ShouldFail_WhenVehicleDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>(), 2021);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vehicle?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find vehicle");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find vehicle by [{input.VehicleId}].",
            Times.Once()
        );
        _vehicleRepositoryMock.Verify(x => x.Update(It.IsAny<Vehicle>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Throw >> When ModelYear is earlier than ManufactureYear")]
    public async Task Handle_ShouldThrow_WhenModelYearIsEarlierThanManufactureYear()
    {
        // Arrange
        var vehicle = CreateVehicle(_fixture.Create<Guid>(), 2020);
        var input = CreateInput(vehicle.Id, 2019);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        // Act
        var act = async () => await _useCase.Handle(input, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _vehicleRepositoryMock.Verify(x => x.Update(It.IsAny<Vehicle>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error updating vehicle")]
    public async Task Handle_ShouldFail_WhenErrorUpdatingVehicle()
    {
        // Arrange
        var vehicle = CreateVehicle(_fixture.Create<Guid>(), 2020);
        var input = CreateInput(vehicle.Id, 2021);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _vehicleRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error updating vehicle with id {input.VehicleId}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error updating vehicle with id {input.VehicleId}.",
            Times.Once()
        );
    }
}
