using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Infrastructure.Repositories;

[Collection("Integration")]
public sealed class ServiceOrderRepositoryTests(DatabaseFixture fixture)
{
    private async Task WithScopeAsync<TRepository>(Func<TRepository, Task> action) where TRepository : notnull
    {
        using var scope = fixture.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<TRepository>());
    }

    private async Task<(Customer Customer, Vehicle Vehicle, User User)> SeedServiceOrderDependenciesAsync()
    {
        var customer = new Customer(
            Guid.NewGuid(), "SO Customer", TestData.ShortString(14), TestData.Email(), TestData.ShortString(15),
            DateTime.UtcNow, DateTime.UtcNow);

        var vehicle = new Vehicle(
            Guid.NewGuid(), customer.Id, TestData.ShortString(10).ToUpperInvariant(), "Brand", "Model",
            2021, 2021, "White", DateTime.UtcNow, DateTime.UtcNow);

        var user = new User(
            Guid.NewGuid(), TestData.Email(), "SO User", TestData.RandomBytes(32), UserRole.User,
            DateTime.UtcNow, DateTime.UtcNow);

        await WithScopeAsync<ICustomerRepository>(async repo =>
        {
            await repo.AddAsync(customer, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        await WithScopeAsync<IVehicleRepository>(async repo =>
        {
            await repo.AddAsync(vehicle, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        await WithScopeAsync<IUserRepository>(async repo =>
        {
            await repo.AddAsync(user, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        return (customer, vehicle, user);
    }

    private static ServiceOrder CreateServiceOrder(Customer customer, Vehicle vehicle, User user, Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        TestData.UniqueNumber(),
        customer.Id,
        vehicle.Id,
        user.Id,
        "Engine noise",
        12000,
        DateTime.UtcNow,
        DateTime.UtcNow,
        DateTime.UtcNow
    );

    private async Task SeedServiceOrderAsync(ServiceOrder serviceOrder) => await WithScopeAsync<IServiceOrderRepository>(async repo =>
    {
        await repo.AddAsync(serviceOrder, CancellationToken.None);
        await repo.UnitOfWork.CommitAsync(CancellationToken.None);
    });

    [Fact(DisplayName = "ServiceOrderRepository >> Should persist and retrieve >> When adding a new service order")]
    public async Task ServiceOrderRepository_ShouldPersistAndRetrieve_WhenAddingNewServiceOrder()
    {
        // Arrange
        var (customer, vehicle, user) = await SeedServiceOrderDependenciesAsync();
        var serviceOrder = CreateServiceOrder(customer, vehicle, user);

        // Act
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
        {
            await repo.AddAsync(serviceOrder, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        ServiceOrder? found = null;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            found = await repo.GetByIdAsync(serviceOrder.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(serviceOrder.Id, found!.Id);
        Assert.Equal(serviceOrder.Number, found.Number);
        Assert.Equal(serviceOrder.CustomerId, found.CustomerId);
        Assert.Equal(serviceOrder.VehicleId, found.VehicleId);
        Assert.Equal(serviceOrder.CreatedBy, found.CreatedBy);
        Assert.Equal(serviceOrder.Status, found.Status);
        Assert.Equal(serviceOrder.ProblemDescription, found.ProblemDescription);
        Assert.Equal(serviceOrder.OdometerReading, found.OdometerReading);
        Assert.Empty(found.Parts);
        Assert.Empty(found.Services);
        Assert.Empty(found.StatusHistory);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should return null >> When service order does not exist")]
    public async Task ServiceOrderRepository_ShouldReturnNull_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        ServiceOrder? found = null;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            found = await repo.GetByIdAsync(id, CancellationToken.None));

        // Assert
        Assert.Null(found);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should persist children >> When service order has parts, services and status history")]
    public async Task ServiceOrderRepository_ShouldPersistChildren_WhenServiceOrderHasPartsServicesAndStatusHistory()
    {
        // Arrange
        var (customer, vehicle, user) = await SeedServiceOrderDependenciesAsync();

        var inventoryItem = new InventoryItem(
            Guid.NewGuid(), TestData.ShortString(20), "Brake Pad", "Brake pad set", 20, 0, 5,
            89.90m, UnitOfMeasure.Piece, true, DateTime.UtcNow, DateTime.UtcNow);

        var service = new Service(
            Guid.NewGuid(), TestData.ShortString(20), "Oil Change", "Oil change service", 120.00m,
            30, true, DateTime.UtcNow, DateTime.UtcNow);

        await WithScopeAsync<IInventoryItemRepository>(async repo =>
        {
            await repo.AddAsync(inventoryItem, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        await WithScopeAsync<IServiceRepository>(async repo =>
        {
            await repo.AddAsync(service, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        var serviceOrder = CreateServiceOrder(customer, vehicle, user);

        var part = new ServiceOrderPart(
            Guid.NewGuid(), serviceOrder.Id, inventoryItem.Id, inventoryItem.Name, inventoryItem.Description,
            inventoryItem.UnitPrice, 2, DateTime.UtcNow, DateTime.UtcNow);

        var orderService = new ServiceOrderService(
            Guid.NewGuid(), serviceOrder.Id, service.Id, service.Name, service.Description,
            service.BasePrice, service.EstimatedDuration, 1, DateTime.UtcNow, DateTime.UtcNow);

        var statusHistory = new ServiceOrderStatusHistory(
            Guid.NewGuid(), serviceOrder.Id, ServiceOrderStatus.Received, ServiceOrderStatus.Diagnosing,
            user.Id, DateTime.UtcNow);

        serviceOrder.AddParts([part]);
        serviceOrder.AddServices([orderService]);
        serviceOrder.AddStatusHistory([statusHistory]);

        // Act
        await SeedServiceOrderAsync(serviceOrder);

        // Assert
        ServiceOrder? found = null;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            found = await repo.GetByIdAsync(serviceOrder.Id, CancellationToken.None));

        Assert.NotNull(found);

        var foundPart = Assert.Single(found!.Parts);
        Assert.Equal(part.Id, foundPart.Id);
        Assert.Equal(part.InventoryItemId, foundPart.InventoryItemId);
        Assert.Equal(part.Quantity, foundPart.Quantity);

        var foundService = Assert.Single(found.Services);
        Assert.Equal(orderService.Id, foundService.Id);
        Assert.Equal(orderService.ServiceId, foundService.ServiceId);

        var foundStatusHistory = Assert.Single(found.StatusHistory);
        Assert.Equal(statusHistory.Id, foundStatusHistory.Id);
        Assert.Equal(statusHistory.PreviousStatus, foundStatusHistory.PreviousStatus);
        Assert.Equal(statusHistory.CurrentStatus, foundStatusHistory.CurrentStatus);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should persist changes >> When updating an existing service order")]
    public async Task ServiceOrderRepository_ShouldPersistChanges_WhenUpdatingExistingServiceOrder()
    {
        // Arrange
        var (customer, vehicle, user) = await SeedServiceOrderDependenciesAsync();
        var serviceOrder = CreateServiceOrder(customer, vehicle, user);
        await SeedServiceOrderAsync(serviceOrder);

        var updated = new ServiceOrder(
            serviceOrder.Id, serviceOrder.Number, serviceOrder.CustomerId, serviceOrder.VehicleId,
            serviceOrder.CreatedBy, serviceOrder.ProblemDescription, serviceOrder.OdometerReading,
            serviceOrder.CreatedAt, DateTime.UtcNow, serviceOrder.OpenedAt, ServiceOrderStatus.Diagnosing,
            "Worn brake pads");

        // Act
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
        {
            repo.Update(updated);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        ServiceOrder? found = null;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            found = await repo.GetByIdAsync(serviceOrder.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(ServiceOrderStatus.Diagnosing, found!.Status);
        Assert.Equal("Worn brake pads", found.DiagnoseDescription);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should remove entity >> When removing an existing service order")]
    public async Task ServiceOrderRepository_ShouldRemoveEntity_WhenRemovingExistingServiceOrder()
    {
        // Arrange
        var (customer, vehicle, user) = await SeedServiceOrderDependenciesAsync();
        var serviceOrder = CreateServiceOrder(customer, vehicle, user);
        await SeedServiceOrderAsync(serviceOrder);

        // Act
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
        {
            repo.Remove(serviceOrder);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        ServiceOrder? found = null;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            found = await repo.GetByIdAsync(serviceOrder.Id, CancellationToken.None));

        Assert.Null(found);
    }
}
