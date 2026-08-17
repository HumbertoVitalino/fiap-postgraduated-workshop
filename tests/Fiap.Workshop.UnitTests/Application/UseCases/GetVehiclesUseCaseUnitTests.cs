using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicles;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicles.Mapper;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetVehiclesUseCaseUnitTests : LoggerTestBase<GetVehiclesUseCase>
{
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock = new();
    private readonly GetVehiclesUseCase _useCase;

    public GetVehiclesUseCaseUnitTests()
    {
        _useCase = new(
            _vehicleRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private static Vehicle CreateVehicle() => new(
        Guid.NewGuid(),
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

    [Fact(DisplayName = "Handle >> Should Success >> When vehicles exist")]
    public async Task Handle_ShouldSuccess_WhenVehiclesExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();
        var vehicles = new[] { CreateVehicle(), CreateVehicle() };

        _vehicleRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync(vehicles);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeEquivalentTo(vehicles.MapToDto());
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "Handle >> Should Success and log >> When vehicles do not exist")]
    public async Task Handle_ShouldSuccessAndLog_WhenVehiclesDoNotExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();

        _vehicleRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync([]);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeAssignableTo<IEnumerable<VehicleResponse>>()
            .Which.Should().BeEmpty();
        result.ErrorMessages.Should().BeEmpty();
        VerifyLog(
            LogLevel.Warning,
            $"[{correlation}] | Unable to find vehicles",
            Times.Once()
        );
    }
}
