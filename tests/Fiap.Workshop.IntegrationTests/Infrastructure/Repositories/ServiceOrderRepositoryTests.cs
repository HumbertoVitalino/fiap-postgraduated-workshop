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
            Guid.NewGuid(), TestData.Email(), "SO User", TestData.ShortString(32), UserRole.Admin,
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
            serviceOrder.Id, serviceOrder.CustomerId, serviceOrder.VehicleId,
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

    [Fact(DisplayName = "ServiceOrderRepository >> Should persist new status history >> When updating an already-persisted service order via UpdateAsync")]
    public async Task ServiceOrderRepository_ShouldPersistNewStatusHistory_WhenUpdatingAlreadyPersistedServiceOrderViaUpdateAsync()
    {
        // Arrange — reproduces the loaded-then-mutated-then-saved flow a use case follows
        // (GetByIdAsync returns an AsNoTracking, disconnected aggregate; StartDiagnosis adds a new
        // child to it in memory; UpdateAsync must reconcile that new child as Added, not Modified).
        var (customer, vehicle, user) = await SeedServiceOrderDependenciesAsync();
        var serviceOrder = CreateServiceOrder(customer, vehicle, user);
        await SeedServiceOrderAsync(serviceOrder);

        ServiceOrder? loaded = null;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            loaded = await repo.GetByIdAsync(serviceOrder.Id, CancellationToken.None));

        loaded!.StartDiagnosis("Worn brake pads", user.Id);

        // Act
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
        {
            await repo.UpdateAsync(loaded, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        ServiceOrder? found = null;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            found = await repo.GetByIdAsync(serviceOrder.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(ServiceOrderStatus.Diagnosing, found!.Status);
        Assert.Equal("Worn brake pads", found.DiagnoseDescription);

        var history = Assert.Single(found.StatusHistory);
        Assert.Equal(ServiceOrderStatus.Received, history.PreviousStatus);
        Assert.Equal(ServiceOrderStatus.Diagnosing, history.CurrentStatus);
        Assert.Equal(user.Id, history.ChangedBy);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should persist new parts and services >> When updating an already-persisted service order via UpdateAsync")]
    public async Task ServiceOrderRepository_ShouldPersistNewPartsAndServices_WhenUpdatingAlreadyPersistedServiceOrderViaUpdateAsync()
    {
        // Arrange — same reconciliation gap as the StatusHistory test above, now exercised for
        // Parts/Services: this is exactly the flow AddBudgetUseCase needs (load OS in Diagnosing,
        // add budget parts/services, persist).
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
        await SeedServiceOrderAsync(serviceOrder);

        ServiceOrder? loaded = null;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            loaded = await repo.GetByIdAsync(serviceOrder.Id, CancellationToken.None));

        loaded!.StartDiagnosis("Worn brake pads", user.Id);

        var part = new ServiceOrderPart(
            Guid.NewGuid(), loaded.Id, inventoryItem.Id, inventoryItem.Name, inventoryItem.Description,
            inventoryItem.UnitPrice, 2, DateTime.UtcNow, DateTime.UtcNow);

        var orderService = new ServiceOrderService(
            Guid.NewGuid(), loaded.Id, service.Id, service.Name, service.Description,
            service.BasePrice, service.EstimatedDuration, 1, DateTime.UtcNow, DateTime.UtcNow);

        loaded.AddBudget([orderService], [part], user.Id);

        // Act
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
        {
            await repo.UpdateAsync(loaded, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        ServiceOrder? found = null;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            found = await repo.GetByIdAsync(serviceOrder.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(ServiceOrderStatus.AwaitingApproval, found!.Status);
        Assert.Equal(299.80m, found.Subtotal);
        Assert.Equal(0m, found.Discount);
        Assert.Equal(299.80m, found.Total);

        var foundPart = Assert.Single(found.Parts);
        Assert.Equal(part.Id, foundPart.Id);
        Assert.Equal(part.InventoryItemId, foundPart.InventoryItemId);

        var foundService = Assert.Single(found.Services);
        Assert.Equal(orderService.Id, foundService.Id);
        Assert.Equal(orderService.ServiceId, foundService.ServiceId);

        Assert.Equal(2, found.StatusHistory.Count);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should persist consecutive updates >> When UpdateAsync is called twice in the same scope for the same service order")]
    public async Task ServiceOrderRepository_ShouldPersistConsecutiveUpdates_WhenUpdateAsyncIsCalledTwiceInTheSameScopeForTheSameServiceOrder()
    {
        // Arrange — reproduces two transition use cases sharing one DbContext within the same
        // request scope (e.g. StartDiagnosis followed by AddBudget): the second UpdateAsync call
        // used to throw ("cannot be tracked because another instance with the same key value is
        // already being tracked") because the StatusHistory row added by the first call stayed
        // tracked in the ChangeTracker and the reconciliation didn't check for that before
        // attaching a fresh disconnected instance with the same Id.
        var (customer, vehicle, user) = await SeedServiceOrderDependenciesAsync();
        var serviceOrder = CreateServiceOrder(customer, vehicle, user);
        await SeedServiceOrderAsync(serviceOrder);

        using var scope = fixture.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IServiceOrderRepository>();

        var loaded = await repo.GetByIdAsync(serviceOrder.Id, CancellationToken.None);
        loaded!.StartDiagnosis("Worn brake pads", user.Id);

        // Act
        await repo.UpdateAsync(loaded, CancellationToken.None);
        await repo.UnitOfWork.CommitAsync(CancellationToken.None);

        loaded.AddBudget([], [], user.Id);
        await repo.UpdateAsync(loaded, CancellationToken.None);
        await repo.UnitOfWork.CommitAsync(CancellationToken.None);

        // Assert
        ServiceOrder? found = null;
        await WithScopeAsync<IServiceOrderRepository>(async r =>
            found = await r.GetByIdAsync(serviceOrder.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(ServiceOrderStatus.AwaitingApproval, found!.Status);
        Assert.Equal(2, found.StatusHistory.Count);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should retrieve all by vehicle id >> When vehicle has service orders")]
    public async Task ServiceOrderRepository_ShouldRetrieveAllByVehicleId_WhenVehicleHasServiceOrders()
    {
        // Arrange
        var (customer, vehicle, user) = await SeedServiceOrderDependenciesAsync();
        var serviceOrder = CreateServiceOrder(customer, vehicle, user);
        await SeedServiceOrderAsync(serviceOrder);

        var (otherCustomer, otherVehicle, otherUser) = await SeedServiceOrderDependenciesAsync();
        var otherServiceOrder = CreateServiceOrder(otherCustomer, otherVehicle, otherUser);
        await SeedServiceOrderAsync(otherServiceOrder);

        // Act
        IReadOnlyCollection<ServiceOrder>? found = null;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            found = await repo.GetAllByVehicleIdAsync(vehicle.Id, CancellationToken.None));

        // Assert
        Assert.NotNull(found);
        var match = Assert.Single(found!);
        Assert.Equal(serviceOrder.Id, match.Id);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should return empty >> When vehicle has no service orders")]
    public async Task ServiceOrderRepository_ShouldReturnEmpty_WhenVehicleHasNoServiceOrders()
    {
        // Act
        IReadOnlyCollection<ServiceOrder>? found = null;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            found = await repo.GetAllByVehicleIdAsync(Guid.NewGuid(), CancellationToken.None));

        // Assert
        Assert.NotNull(found);
        Assert.Empty(found!);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should return true >> When vehicle has a service order")]
    public async Task ServiceOrderRepository_ShouldReturnTrue_WhenVehicleHasServiceOrder()
    {
        // Arrange
        var (customer, vehicle, user) = await SeedServiceOrderDependenciesAsync();
        var serviceOrder = CreateServiceOrder(customer, vehicle, user);
        await SeedServiceOrderAsync(serviceOrder);

        // Act
        var exists = false;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            exists = await repo.ExistsWithVehicleIdAsync(vehicle.Id, CancellationToken.None));

        // Assert
        Assert.True(exists);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should return false >> When vehicle has no service orders")]
    public async Task ServiceOrderRepository_ShouldReturnFalse_WhenVehicleHasNoServiceOrders()
    {
        // Act
        var exists = true;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            exists = await repo.ExistsWithVehicleIdAsync(Guid.NewGuid(), CancellationToken.None));

        // Assert
        Assert.False(exists);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should return true >> When service has been used in a service order")]
    public async Task ServiceOrderRepository_ShouldReturnTrue_WhenServiceHasBeenUsedInServiceOrder()
    {
        // Arrange
        var (customer, vehicle, user) = await SeedServiceOrderDependenciesAsync();

        var service = new Service(
            Guid.NewGuid(), TestData.ShortString(20), "Oil Change", "Oil change service", 120.00m,
            30, true, DateTime.UtcNow, DateTime.UtcNow);

        await WithScopeAsync<IServiceRepository>(async repo =>
        {
            await repo.AddAsync(service, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        var serviceOrder = CreateServiceOrder(customer, vehicle, user);

        var orderService = new ServiceOrderService(
            Guid.NewGuid(), serviceOrder.Id, service.Id, service.Name, service.Description,
            service.BasePrice, service.EstimatedDuration, 1, DateTime.UtcNow, DateTime.UtcNow);

        serviceOrder.AddServices([orderService]);
        await SeedServiceOrderAsync(serviceOrder);

        // Act
        var exists = false;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            exists = await repo.ExistsWithServiceIdAsync(service.Id, CancellationToken.None));

        // Assert
        Assert.True(exists);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should return false >> When service has never been used in a service order")]
    public async Task ServiceOrderRepository_ShouldReturnFalse_WhenServiceHasNeverBeenUsedInServiceOrder()
    {
        // Act
        var exists = true;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            exists = await repo.ExistsWithServiceIdAsync(Guid.NewGuid(), CancellationToken.None));

        // Assert
        Assert.False(exists);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should return true >> When inventory item has been used in a service order")]
    public async Task ServiceOrderRepository_ShouldReturnTrue_WhenInventoryItemHasBeenUsedInServiceOrder()
    {
        // Arrange
        var (customer, vehicle, user) = await SeedServiceOrderDependenciesAsync();

        var inventoryItem = new InventoryItem(
            Guid.NewGuid(), TestData.ShortString(20), "Brake Pad", "Brake pad set", 20, 0, 5,
            89.90m, UnitOfMeasure.Piece, true, DateTime.UtcNow, DateTime.UtcNow);

        await WithScopeAsync<IInventoryItemRepository>(async repo =>
        {
            await repo.AddAsync(inventoryItem, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        var serviceOrder = CreateServiceOrder(customer, vehicle, user);

        var orderPart = new ServiceOrderPart(
            Guid.NewGuid(), serviceOrder.Id, inventoryItem.Id, inventoryItem.Name, inventoryItem.Description,
            inventoryItem.UnitPrice, 1, DateTime.UtcNow, DateTime.UtcNow);

        serviceOrder.AddParts([orderPart]);
        await SeedServiceOrderAsync(serviceOrder);

        // Act
        var exists = false;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            exists = await repo.ExistsWithInventoryItemIdAsync(inventoryItem.Id, CancellationToken.None));

        // Assert
        Assert.True(exists);
    }

    [Fact(DisplayName = "ServiceOrderRepository >> Should return false >> When inventory item has never been used in a service order")]
    public async Task ServiceOrderRepository_ShouldReturnFalse_WhenInventoryItemHasNeverBeenUsedInServiceOrder()
    {
        // Act
        var exists = true;
        await WithScopeAsync<IServiceOrderRepository>(async repo =>
            exists = await repo.ExistsWithInventoryItemIdAsync(Guid.NewGuid(), CancellationToken.None));

        // Assert
        Assert.False(exists);
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
