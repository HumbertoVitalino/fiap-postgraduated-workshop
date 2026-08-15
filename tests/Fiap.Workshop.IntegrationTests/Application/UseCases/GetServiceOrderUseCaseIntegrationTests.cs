using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class GetServiceOrderUseCaseIntegrationTests(DatabaseFixture fixture)
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
            Guid.NewGuid(), TestData.Email(), "Attendant", TestData.ShortString(32), UserRole.Attendant,
            DateTime.Now, DateTime.Now);

        using var scope = fixture.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        await userRepository.AddAsync(user, CancellationToken.None);
        await userRepository.UnitOfWork.CommitAsync(CancellationToken.None);

        return user.Id;
    }

    private async Task<ServiceOrderResponse> CreateServiceOrderAsync()
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
        var input = new CreateServiceOrderInput(Guid.NewGuid(), customerId, vehicleId, createdBy, "Engine noise", 12000);
        var result = await createServiceOrderUseCase.Handle(input, CancellationToken.None);
        return result.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
    }

    private async Task<Output> HandleAsync(GetServiceOrderInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetServiceOrderUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "GetServiceOrderUseCase >> Should return service order >> When service order exists")]
    public async Task Handle_ShouldReturnServiceOrder_WhenServiceOrderExists()
    {
        // Arrange
        var created = await CreateServiceOrderAsync();
        var input = new GetServiceOrderInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var found = result.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
        found.Id.Should().Be(created.Id);
        found.CustomerId.Should().Be(created.CustomerId);
        found.VehicleId.Should().Be(created.VehicleId);
        found.Status.Should().Be(ServiceOrderStatus.Received.ToString());
        found.Parts.Should().BeEmpty();
        found.Services.Should().BeEmpty();
        found.StatusHistory.Should().BeEmpty();
    }

    [Fact(DisplayName = "GetServiceOrderUseCase >> Should return budget items and status history >> When service order has parts, services and status history")]
    public async Task Handle_ShouldReturnBudgetItemsAndStatusHistory_WhenServiceOrderHasPartsServicesAndStatusHistory()
    {
        // Arrange — service order is built with its children already attached before the first
        // AddAsync, same pattern as ServiceOrderRepositoryTests. Loading an already-persisted
        // order, calling AddParts/AddServices/AddStatusHistory on it and then Update()-ing it is
        // NOT exercised here: Repository<T>.Upsert falls back to DbSet.Update() for aggregates
        // that EF hasn't already tracked, which marks new children with a pre-assigned Guid Id as
        // Modified (not Added) and silently no-ops their INSERT — a real gap that ticket 20
        // (AddBudget) will need to fix before it can add children to an existing ServiceOrder.
        Guid customerId, vehicleId;
        using (var scope = fixture.Services.CreateScope())
        {
            var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
            var customerResult = await createCustomerUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
            customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

            var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
            var vehicleResult = await createVehicleUseCase.Handle(CreateVehicleInput(customerId), CancellationToken.None);
            vehicleId = vehicleResult.Result.Should().BeOfType<VehicleResponse>().Subject.Id;
        }

        var createdBy = await CreateUserAsync();

        var inventoryItem = new InventoryItem(
            Guid.NewGuid(), TestData.ShortString(20), "Brake Pad", "Brake pad set", 20, 0, 5,
            89.90m, UnitOfMeasure.Piece, true, DateTime.Now, DateTime.Now);

        var service = new Service(
            Guid.NewGuid(), TestData.ShortString(20), "Oil Change", "Oil change service", 120.00m,
            30, true, DateTime.Now, DateTime.Now);

        using (var scope = fixture.Services.CreateScope())
        {
            var inventoryItemRepository = scope.ServiceProvider.GetRequiredService<IInventoryItemRepository>();
            await inventoryItemRepository.AddAsync(inventoryItem, CancellationToken.None);
            await inventoryItemRepository.UnitOfWork.CommitAsync(CancellationToken.None);

            var serviceRepository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
            await serviceRepository.AddAsync(service, CancellationToken.None);
            await serviceRepository.UnitOfWork.CommitAsync(CancellationToken.None);
        }

        var serviceOrder = new ServiceOrder(
            Guid.NewGuid(), customerId, vehicleId, createdBy, "Engine noise", 12000,
            DateTime.Now, DateTime.Now, DateTime.Now);

        var part = new ServiceOrderPart(
            Guid.NewGuid(), serviceOrder.Id, inventoryItem.Id, inventoryItem.Name, inventoryItem.Description,
            inventoryItem.UnitPrice, 2, DateTime.Now, DateTime.Now);

        var orderService = new ServiceOrderService(
            Guid.NewGuid(), serviceOrder.Id, service.Id, service.Name, service.Description,
            service.BasePrice, service.EstimatedDuration, 1, DateTime.Now, DateTime.Now);

        var statusHistory = new ServiceOrderStatusHistory(
            Guid.NewGuid(), serviceOrder.Id, ServiceOrderStatus.Received, ServiceOrderStatus.Diagnosing,
            createdBy, DateTime.Now);

        serviceOrder.AddParts([part]);
        serviceOrder.AddServices([orderService]);
        serviceOrder.AddStatusHistory([statusHistory]);

        using (var scope = fixture.Services.CreateScope())
        {
            var serviceOrderRepository = scope.ServiceProvider.GetRequiredService<IServiceOrderRepository>();
            await serviceOrderRepository.AddAsync(serviceOrder, CancellationToken.None);
            await serviceOrderRepository.UnitOfWork.CommitAsync(CancellationToken.None);
        }

        var input = new GetServiceOrderInput(Guid.NewGuid(), serviceOrder.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var found = result.Result.Should().BeOfType<ServiceOrderResponse>().Subject;

        var foundPart = found.Parts.Should().ContainSingle().Subject;
        foundPart.Id.Should().Be(part.Id);
        foundPart.InventoryItemId.Should().Be(inventoryItem.Id);
        foundPart.Quantity.Should().Be(2);

        var foundService = found.Services.Should().ContainSingle().Subject;
        foundService.Id.Should().Be(orderService.Id);
        foundService.ServiceId.Should().Be(service.Id);

        var foundStatusHistory = found.StatusHistory.Should().ContainSingle().Subject;
        foundStatusHistory.Id.Should().Be(statusHistory.Id);
        foundStatusHistory.PreviousStatus.Should().Be(ServiceOrderStatus.Received.ToString());
        foundStatusHistory.CurrentStatus.Should().Be(ServiceOrderStatus.Diagnosing.ToString());
    }

    [Fact(DisplayName = "GetServiceOrderUseCase >> Should fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var input = new GetServiceOrderInput(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service order");
        result.Result.Should().BeNull();
    }
}
