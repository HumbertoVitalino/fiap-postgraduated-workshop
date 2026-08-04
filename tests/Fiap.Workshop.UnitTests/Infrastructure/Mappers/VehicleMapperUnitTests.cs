using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Xunit;

namespace Fiap.Workshop.UnitTests.Infrastructure.Mappers;

public class VehicleMapperUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "Vehicle >> Should map to model >> When mapping from domain")]
    public void Vehicle_ShouldMapToModel_WhenMappingFromDomain()
    {
        // Arrange
        var vehicle = new Vehicle(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<int>(),
            _fixture.Create<int>(),
            _fixture.Create<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var model = vehicle.MapToModel();

        // Assert
        Assert.Equal(vehicle.Id, model.Id);
        Assert.Equal(vehicle.CustomerId, model.CustomerId);
        Assert.Equal(vehicle.LicensePlate, model.LicensePlate);
        Assert.Equal(vehicle.Brand, model.Brand);
        Assert.Equal(vehicle.Model, model.Model);
        Assert.Equal(vehicle.ManufactureYear, model.ManufactureYear);
        Assert.Equal(vehicle.ModelYear, model.ModelYear);
        Assert.Equal(vehicle.Color, model.Color);
        Assert.Equal(vehicle.CreatedAt, model.CreatedAt);
        Assert.Equal(vehicle.UpdatedAt, model.UpdatedAt);
    }

    [Fact(DisplayName = "VehicleModel >> Should map to domain >> When mapping from model")]
    public void VehicleModel_ShouldMapToDomain_WhenMappingFromModel()
    {
        // Arrange
        var model = new VehicleModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<int>(),
            _fixture.Create<int>(),
            _fixture.Create<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var vehicle = model.MapToDomain();

        // Assert
        Assert.Equal(model.Id, vehicle.Id);
        Assert.Equal(model.CustomerId, vehicle.CustomerId);
        Assert.Equal(model.LicensePlate, vehicle.LicensePlate);
        Assert.Equal(model.Brand, vehicle.Brand);
        Assert.Equal(model.Model, vehicle.Model);
        Assert.Equal(model.ManufactureYear, vehicle.ManufactureYear);
        Assert.Equal(model.ModelYear, vehicle.ModelYear);
        Assert.Equal(model.Color, vehicle.Color);
        Assert.Equal(model.CreatedAt, vehicle.CreatedAt);
        Assert.Equal(model.UpdatedAt, vehicle.UpdatedAt);
    }
}
