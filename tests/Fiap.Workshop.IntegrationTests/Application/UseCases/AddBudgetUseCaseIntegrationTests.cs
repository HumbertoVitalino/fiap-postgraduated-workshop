using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.StartDiagnosis.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class AddBudgetUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateCustomerInput CreateCustomerInput() => new(
        Guid.NewGuid(),
        "Service Order Owner",
        TestData.Document(),
        TestData.Email(),
        TestData.ShortString(15)
    );

    private static CreateVehicleInput CreateVehicleInput(Guid customerId) => new(
        Guid.NewGuid(),
        customerId,
        TestData.LicensePlate(),
        "Ford",
        "Ka",
        2020,
        2021,
        "Black"
    );

    private async Task<Guid> CreateUserAsync()
    {
        var user = new User(
            Guid.NewGuid(), TestData.Email(), "Mechanic", TestData.ShortString(32), UserRole.Mechanic,
            DateTime.Now, DateTime.Now);

        using var scope = fixture.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        await userRepository.AddAsync(user, CancellationToken.None);
        await userRepository.UnitOfWork.CommitAsync(CancellationToken.None);

        return user.Id;
    }

    private async Task<InventoryItem> CreateInventoryItemAsync(int quantityOnHand)
    {
        var inventoryItem = new InventoryItem(
            Guid.NewGuid(), TestData.ShortString(20), "Brake Pad", "Brake pad set", quantityOnHand, 0, 5,
            89.90m, UnitOfMeasure.Piece, true, DateTime.Now, DateTime.Now);

        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInventoryItemRepository>();
        await repository.AddAsync(inventoryItem, CancellationToken.None);
        await repository.UnitOfWork.CommitAsync(CancellationToken.None);

        return inventoryItem;
    }

    private async Task<Service> CreateServiceAsync()
    {
        var service = new Service(
            Guid.NewGuid(), TestData.ShortString(20), "Oil Change", "Oil change service", 120.00m,
            30, true, DateTime.Now, DateTime.Now);

        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        await repository.AddAsync(service, CancellationToken.None);
        await repository.UnitOfWork.CommitAsync(CancellationToken.None);

        return service;
    }

    private async Task<InventoryItem> CreateInactiveInventoryItemAsync(int quantityOnHand)
    {
        var inventoryItem = new InventoryItem(
            Guid.NewGuid(), TestData.ShortString(20), "Discontinued Part", "No longer sold", quantityOnHand, 0, 5,
            89.90m, UnitOfMeasure.Piece, false, DateTime.Now, DateTime.Now);

        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInventoryItemRepository>();
        await repository.AddAsync(inventoryItem, CancellationToken.None);
        await repository.UnitOfWork.CommitAsync(CancellationToken.None);

        return inventoryItem;
    }

    private async Task<Service> CreateInactiveServiceAsync()
    {
        var service = new Service(
            Guid.NewGuid(), TestData.ShortString(20), "Discontinued Service", "No longer offered", 120.00m,
            30, false, DateTime.Now, DateTime.Now);

        using var scope = fixture.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        await repository.AddAsync(service, CancellationToken.None);
        await repository.UnitOfWork.CommitAsync(CancellationToken.None);

        return service;
    }

    private async Task<ServiceOrderResponse> CreateDiagnosingServiceOrderAsync()
    {
        using var scope = fixture.Services.CreateScope();

        var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var customerResult = await createCustomerUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
        var customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var vehicleResult = await createVehicleUseCase.Handle(CreateVehicleInput(customerId), CancellationToken.None);
        var vehicleId = vehicleResult.Result.Should().BeOfType<VehicleResponse>().Subject.Id;

        var createdBy = await CreateUserAsync();

        var createServiceOrderUseCase = scope.ServiceProvider.GetRequiredService<ICreateServiceOrderUseCase>();
        var createInput = new CreateServiceOrderInput(Guid.NewGuid(), customerId, vehicleId, createdBy, "Engine noise", 12000);
        var createResult = await createServiceOrderUseCase.Handle(createInput, CancellationToken.None);
        var created = createResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;

        var startDiagnosisUseCase = scope.ServiceProvider.GetRequiredService<IStartDiagnosisUseCase>();
        var diagnosisInput = new StartDiagnosisInput(Guid.NewGuid(), created.Id, createdBy, "Worn brake pads");
        var diagnosisResult = await startDiagnosisUseCase.Handle(diagnosisInput, CancellationToken.None);
        return diagnosisResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
    }

    private async Task<Output> HandleAsync(AddBudgetInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IAddBudgetUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "AddBudgetUseCase >> Should persist budget and reserve stock >> When service order is Diagnosing and stock is available")]
    public async Task Handle_ShouldPersistBudgetAndReserveStock_WhenServiceOrderIsDiagnosingAndStockIsAvailable()
    {
        // Arrange
        var serviceOrder = await CreateDiagnosingServiceOrderAsync();
        var inventoryItem = await CreateInventoryItemAsync(20);
        var service = await CreateServiceAsync();
        var changedBy = await CreateUserAsync();

        var input = new AddBudgetInput(
            Guid.NewGuid(),
            serviceOrder.Id,
            changedBy,
            [new AddBudgetServiceItem(service.Id, 1)],
            [new AddBudgetPartItem(inventoryItem.Id, 2)]
        );

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var updated = result.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
        updated.Status.Should().Be(ServiceOrderStatus.AwaitingApproval.ToString());
        updated.Subtotal.Should().Be(299.80m);
        updated.Discount.Should().Be(0m);
        updated.Total.Should().Be(299.80m);

        using var scope = fixture.Services.CreateScope();

        var serviceOrderRepository = scope.ServiceProvider.GetRequiredService<IServiceOrderRepository>();
        var foundOrder = await serviceOrderRepository.GetByIdAsync(serviceOrder.Id, CancellationToken.None);
        foundOrder.Should().NotBeNull();
        foundOrder!.Parts.Should().ContainSingle();
        foundOrder.Services.Should().ContainSingle();
        foundOrder.StatusHistory.Should().HaveCount(2);

        var inventoryItemRepository = scope.ServiceProvider.GetRequiredService<IInventoryItemRepository>();
        var foundInventoryItem = await inventoryItemRepository.GetByIdAsync(inventoryItem.Id, CancellationToken.None);
        foundInventoryItem.Should().NotBeNull();
        foundInventoryItem!.ReservedQuantity.Should().Be(2);
    }

    [Fact(DisplayName = "AddBudgetUseCase >> Should fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var input = new AddBudgetInput(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [], []);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service order");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "AddBudgetUseCase >> Should fail >> When inventory item does not exist")]
    public async Task Handle_ShouldFail_WhenInventoryItemDoesNotExist()
    {
        // Arrange
        var serviceOrder = await CreateDiagnosingServiceOrderAsync();
        var changedBy = await CreateUserAsync();
        var input = new AddBudgetInput(Guid.NewGuid(), serviceOrder.Id, changedBy, [], [new AddBudgetPartItem(Guid.NewGuid(), 1)]);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find inventory item");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "AddBudgetUseCase >> Should fail >> When service is inactive")]
    public async Task Handle_ShouldFail_WhenServiceIsInactive()
    {
        // Arrange
        var serviceOrder = await CreateDiagnosingServiceOrderAsync();
        var service = await CreateInactiveServiceAsync();
        var changedBy = await CreateUserAsync();
        var input = new AddBudgetInput(Guid.NewGuid(), serviceOrder.Id, changedBy, [new AddBudgetServiceItem(service.Id, 1)], []);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Service is inactive and cannot be added to a budget.");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "AddBudgetUseCase >> Should fail >> When inventory item is inactive")]
    public async Task Handle_ShouldFail_WhenInventoryItemIsInactive()
    {
        // Arrange
        var serviceOrder = await CreateDiagnosingServiceOrderAsync();
        var inventoryItem = await CreateInactiveInventoryItemAsync(20);
        var changedBy = await CreateUserAsync();
        var input = new AddBudgetInput(Guid.NewGuid(), serviceOrder.Id, changedBy, [], [new AddBudgetPartItem(inventoryItem.Id, 1)]);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Inventory item is inactive and cannot be added to a budget.");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "AddBudgetUseCase >> Should throw >> When requested quantity exceeds available stock")]
    public async Task Handle_ShouldThrow_WhenRequestedQuantityExceedsAvailableStock()
    {
        // Arrange
        var serviceOrder = await CreateDiagnosingServiceOrderAsync();
        var inventoryItem = await CreateInventoryItemAsync(1);
        var changedBy = await CreateUserAsync();
        var input = new AddBudgetInput(Guid.NewGuid(), serviceOrder.Id, changedBy, [], [new AddBudgetPartItem(inventoryItem.Id, 2)]);

        // Act
        var act = async () => await HandleAsync(input);

        // Assert
        await act.Should().ThrowAsync<Fiap.Workshop.Domain.Abstractions.DomainException>();
    }

    [Fact(DisplayName = "AddBudgetUseCase >> Should throw >> When service order is not Diagnosing")]
    public async Task Handle_ShouldThrow_WhenServiceOrderIsNotDiagnosing()
    {
        // Arrange — CreateServiceOrder alone leaves the order in Received, not Diagnosing.
        using var scope = fixture.Services.CreateScope();
        var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var customerResult = await createCustomerUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
        var customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var vehicleResult = await createVehicleUseCase.Handle(CreateVehicleInput(customerId), CancellationToken.None);
        var vehicleId = vehicleResult.Result.Should().BeOfType<VehicleResponse>().Subject.Id;

        var createdBy = await CreateUserAsync();

        var createServiceOrderUseCase = scope.ServiceProvider.GetRequiredService<ICreateServiceOrderUseCase>();
        var createInput = new CreateServiceOrderInput(Guid.NewGuid(), customerId, vehicleId, createdBy, "Engine noise", 12000);
        var createResult = await createServiceOrderUseCase.Handle(createInput, CancellationToken.None);
        var created = createResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;

        var input = new AddBudgetInput(Guid.NewGuid(), created.Id, createdBy, [], []);

        // Act
        var act = async () => await HandleAsync(input);

        // Assert
        await act.Should().ThrowAsync<Fiap.Workshop.Domain.Abstractions.DomainException>();
    }
}
