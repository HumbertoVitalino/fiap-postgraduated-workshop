using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class VehicleUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "Vehicle >> Should be created >> When all required properties are provided")]
    public void Vehicle_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var licensePlate = _fixture.Create<string>();
        var brand = _fixture.Create<string>();
        var model = _fixture.Create<string>();
        var manufactureYear = _fixture.Create<int>();
        var modelYear = _fixture.Create<int>();
        var color = _fixture.Create<string>();
        var createdAt = DateTime.Now;
        var updatedAt = DateTime.Now;

        // Act
        var vehicle = new Vehicle(
            id,
            customerId,
            licensePlate,
            brand,
            model,
            manufactureYear,
            modelYear,
            color,
            createdAt,
            updatedAt
        );

        // Assert
        Assert.Equal(id, vehicle.Id);
        Assert.Equal(customerId, vehicle.CustomerId);
        Assert.Equal(licensePlate, vehicle.LicensePlate);
        Assert.Equal(brand, vehicle.Brand);
        Assert.Equal(model, vehicle.Model);
        Assert.Equal(manufactureYear, vehicle.ManufactureYear);
        Assert.Equal(modelYear, vehicle.ModelYear);
        Assert.Equal(color, vehicle.Color);
        Assert.Equal(createdAt, vehicle.CreatedAt);
        Assert.Equal(updatedAt, vehicle.UpdatedAt);
    }
}
