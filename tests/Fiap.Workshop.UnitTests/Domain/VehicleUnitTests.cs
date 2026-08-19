using AutoFixture;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Errors;
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
        var modelYear = manufactureYear + 1;
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

    [Fact(DisplayName = "Vehicle >> Should throw >> When ModelYear is earlier than ManufactureYear")]
    public void Vehicle_ShouldThrow_WhenModelYearIsEarlierThanManufactureYear()
    {
        // Arrange
        var manufactureYear = _fixture.Create<int>();
        var modelYear = manufactureYear - 1;

        // Act
        var act = () => new Vehicle(
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            manufactureYear,
            modelYear,
            _fixture.Create<string>(),
            DateTime.Now,
            DateTime.Now
        );

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(VehicleErrors.InvalidModelYear, exception.Message);
    }

    [Fact(DisplayName = "Vehicle >> Should update profile >> When UpdateProfile is called")]
    public void Vehicle_ShouldUpdateProfile_WhenUpdateProfileIsCalled()
    {
        // Arrange
        var manufactureYear = 2020;
        var vehicle = new Vehicle(
            Guid.NewGuid(),
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
        var originalUpdatedAt = vehicle.UpdatedAt;
        var originalLicensePlate = vehicle.LicensePlate;
        var originalManufactureYear = vehicle.ManufactureYear;

        // Act
        vehicle.UpdateProfile("Toyota", "Corolla", "White", manufactureYear + 1);

        // Assert
        Assert.Equal("Toyota", vehicle.Brand);
        Assert.Equal("Corolla", vehicle.Model);
        Assert.Equal("White", vehicle.Color);
        Assert.Equal(manufactureYear + 1, vehicle.ModelYear);
        Assert.Equal(originalLicensePlate, vehicle.LicensePlate);
        Assert.Equal(originalManufactureYear, vehicle.ManufactureYear);
        Assert.True(vehicle.UpdatedAt >= originalUpdatedAt);
    }

    [Fact(DisplayName = "Vehicle >> Should throw >> When UpdateProfile sets ModelYear earlier than ManufactureYear")]
    public void Vehicle_ShouldThrow_WhenUpdateProfileSetsModelYearEarlierThanManufactureYear()
    {
        // Arrange
        var manufactureYear = 2020;
        var vehicle = new Vehicle(
            Guid.NewGuid(),
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

        // Act
        var act = () => vehicle.UpdateProfile("Ford", "Ka", "Black", manufactureYear - 1);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(VehicleErrors.InvalidModelYear, exception.Message);
    }
}
