using AutoFixture;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicle;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicle.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetVehicleUseCaseUnitTests : LoggerTestBase<GetVehicleUseCase>
{
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly GetVehicleUseCase _useCase;

    public GetVehicleUseCaseUnitTests()
    {
        _useCase = new(
            _vehicleRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private static Vehicle CreateVehicle(Guid id) => new(
        id,
        Guid.NewGuid(),
        "ABC1234",
        "Ford",
        "Ka",
        2020,
        2021,
        "Black",
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When vehicle exists")]
    public async Task Handle_ShouldSuccess_WhenVehicleExists()
    {
        // Arrange
        var vehicle = CreateVehicle(_fixture.Create<Guid>());
        var input = new GetVehicleInput(_fixture.Create<Guid>(), vehicle.Id);

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<VehicleResponse>().Which;
        response.Id.Should().Be(vehicle.Id);
        response.LicensePlate.Should().Be(vehicle.LicensePlate);
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When vehicle does not exist")]
    public async Task Handle_ShouldFail_WhenVehicleDoesNotExist()
    {
        // Arrange
        var input = new GetVehicleInput(_fixture.Create<Guid>(), _fixture.Create<Guid>());

        _vehicleRepositoryMock.Setup(x => x.GetByIdAsync(input.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(null);

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
    }
}
