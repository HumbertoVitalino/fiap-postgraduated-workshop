using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Infrastructure.Repositories;

[Collection("Integration")]
public sealed class VehicleRepositoryTests(DatabaseFixture fixture)
{
    private static Customer CreateCustomer() => new(
        Guid.NewGuid(),
        "Vehicle Owner",
        TestData.ShortString(14),
        TestData.Email(),
        TestData.ShortString(15),
        DateTime.UtcNow,
        DateTime.UtcNow
    );

    private static Vehicle CreateVehicle(Guid customerId, Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        customerId,
        TestData.ShortString(10).ToUpperInvariant(),
        "Test Brand",
        "Test Model",
        2020,
        2020,
        "Black",
        DateTime.UtcNow,
        DateTime.UtcNow
    );

    private async Task WithScopeAsync(Func<ICustomerRepository, Task> action)
    {
        using var scope = fixture.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<ICustomerRepository>());
    }

    private async Task WithScopeAsync(Func<IVehicleRepository, Task> action)
    {
        using var scope = fixture.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<IVehicleRepository>());
    }

    private async Task<Customer> SeedCustomerAsync()
    {
        var customer = CreateCustomer();

        await WithScopeAsync(async (ICustomerRepository repo) =>
        {
            await repo.AddAsync(customer, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        return customer;
    }

    private async Task SeedVehicleAsync(Vehicle vehicle) => await WithScopeAsync(async (IVehicleRepository repo) =>
    {
        await repo.AddAsync(vehicle, CancellationToken.None);
        await repo.UnitOfWork.CommitAsync(CancellationToken.None);
    });

    [Fact(DisplayName = "VehicleRepository >> Should persist and retrieve >> When adding a new vehicle")]
    public async Task VehicleRepository_ShouldPersistAndRetrieve_WhenAddingNewVehicle()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var vehicle = CreateVehicle(customer.Id);

        // Act
        await WithScopeAsync(async (IVehicleRepository repo) =>
        {
            await repo.AddAsync(vehicle, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        Vehicle? found = null;
        await WithScopeAsync(async (IVehicleRepository repo) => found = await repo.GetByIdAsync(vehicle.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(vehicle.Id, found!.Id);
        Assert.Equal(vehicle.CustomerId, found.CustomerId);
        Assert.Equal(vehicle.LicensePlate, found.LicensePlate);
        Assert.Equal(vehicle.Brand, found.Brand);
        Assert.Equal(vehicle.Model, found.Model);
        Assert.Equal(vehicle.ManufactureYear, found.ManufactureYear);
        Assert.Equal(vehicle.ModelYear, found.ModelYear);
        Assert.Equal(vehicle.Color, found.Color);
    }

    [Fact(DisplayName = "VehicleRepository >> Should return null >> When vehicle does not exist")]
    public async Task VehicleRepository_ShouldReturnNull_WhenVehicleDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        Vehicle? found = null;
        await WithScopeAsync(async (IVehicleRepository repo) => found = await repo.GetByIdAsync(id, CancellationToken.None));

        // Assert
        Assert.Null(found);
    }

    [Fact(DisplayName = "VehicleRepository >> Should persist changes >> When updating an existing vehicle")]
    public async Task VehicleRepository_ShouldPersistChanges_WhenUpdatingExistingVehicle()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var vehicle = CreateVehicle(customer.Id);
        await SeedVehicleAsync(vehicle);

        var updated = new Vehicle(
            vehicle.Id, vehicle.CustomerId, vehicle.LicensePlate, vehicle.Brand, "Updated Model",
            vehicle.ManufactureYear, vehicle.ModelYear, vehicle.Color, vehicle.CreatedAt, DateTime.UtcNow);

        // Act
        await WithScopeAsync(async (IVehicleRepository repo) =>
        {
            repo.Update(updated);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        Vehicle? found = null;
        await WithScopeAsync(async (IVehicleRepository repo) => found = await repo.GetByIdAsync(vehicle.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal("Updated Model", found!.Model);
    }

    [Fact(DisplayName = "VehicleRepository >> Should retrieve by license plate >> When vehicle exists")]
    public async Task VehicleRepository_ShouldRetrieveByLicensePlate_WhenVehicleExists()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var vehicle = CreateVehicle(customer.Id);
        await SeedVehicleAsync(vehicle);

        // Act
        Vehicle? found = null;
        await WithScopeAsync(async (IVehicleRepository repo) =>
            found = await repo.GetByLicensePlateAsync(vehicle.LicensePlate, CancellationToken.None));

        // Assert
        Assert.NotNull(found);
        Assert.Equal(vehicle.Id, found!.Id);
        Assert.Equal(vehicle.LicensePlate, found.LicensePlate);
    }

    [Fact(DisplayName = "VehicleRepository >> Should return null >> When license plate does not exist")]
    public async Task VehicleRepository_ShouldReturnNull_WhenLicensePlateDoesNotExist()
    {
        // Act
        Vehicle? found = null;
        await WithScopeAsync(async (IVehicleRepository repo) =>
            found = await repo.GetByLicensePlateAsync(TestData.ShortString(10).ToUpperInvariant(), CancellationToken.None));

        // Assert
        Assert.Null(found);
    }

    [Fact(DisplayName = "VehicleRepository >> Should remove entity >> When removing an existing vehicle")]
    public async Task VehicleRepository_ShouldRemoveEntity_WhenRemovingExistingVehicle()
    {
        // Arrange
        var customer = await SeedCustomerAsync();
        var vehicle = CreateVehicle(customer.Id);
        await SeedVehicleAsync(vehicle);

        // Act
        await WithScopeAsync(async (IVehicleRepository repo) =>
        {
            repo.Remove(vehicle);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        Vehicle? found = null;
        await WithScopeAsync(async (IVehicleRepository repo) => found = await repo.GetByIdAsync(vehicle.Id, CancellationToken.None));

        Assert.Null(found);
    }
}
