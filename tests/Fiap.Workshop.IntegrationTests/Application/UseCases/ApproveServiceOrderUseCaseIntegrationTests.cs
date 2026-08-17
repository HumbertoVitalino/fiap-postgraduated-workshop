using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ApproveServiceOrder.Boundaries;
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
public sealed class ApproveServiceOrderUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<Guid> CreateUserAsync(UserRole role)
    {
        var user = new User(
            Guid.NewGuid(), TestData.Email(), role.ToString(), TestData.ShortString(32), role,
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

    private async Task<ServiceOrderResponse> CreateAwaitingApprovalServiceOrderAsync(InventoryItem inventoryItem, int quantity)
    {
        using var scope = fixture.Services.CreateScope();

        var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var customerResult = await createCustomerUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
        var customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var vehicleResult = await createVehicleUseCase.Handle(CreateVehicleInput(customerId), CancellationToken.None);
        var vehicleId = vehicleResult.Result.Should().BeOfType<VehicleResponse>().Subject.Id;

        var mechanicId = await CreateUserAsync(UserRole.Mechanic);

        var createServiceOrderUseCase = scope.ServiceProvider.GetRequiredService<ICreateServiceOrderUseCase>();
        var createInput = new CreateServiceOrderInput(Guid.NewGuid(), customerId, vehicleId, mechanicId, "Engine noise", 12000);
        var createResult = await createServiceOrderUseCase.Handle(createInput, CancellationToken.None);
        var created = createResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;

        var startDiagnosisUseCase = scope.ServiceProvider.GetRequiredService<IStartDiagnosisUseCase>();
        var diagnosisInput = new StartDiagnosisInput(Guid.NewGuid(), created.Id, mechanicId, "Worn brake pads");
        await startDiagnosisUseCase.Handle(diagnosisInput, CancellationToken.None);

        var addBudgetUseCase = scope.ServiceProvider.GetRequiredService<IAddBudgetUseCase>();
        var budgetInput = new AddBudgetInput(
            Guid.NewGuid(), created.Id, mechanicId, [], [new AddBudgetPartItem(inventoryItem.Id, quantity)]);
        var budgetResult = await addBudgetUseCase.Handle(budgetInput, CancellationToken.None);
        return budgetResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
    }

    private async Task<Output> HandleAsync(ApproveServiceOrderInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IApproveServiceOrderUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "ApproveServiceOrderUseCase >> Should commit stock and move to InProgress >> When service order is AwaitingApproval")]
    public async Task Handle_ShouldCommitStockAndMoveToInProgress_WhenServiceOrderIsAwaitingApproval()
    {
        // Arrange
        var inventoryItem = await CreateInventoryItemAsync(20);
        var serviceOrder = await CreateAwaitingApprovalServiceOrderAsync(inventoryItem, 3);
        var attendantId = await CreateUserAsync(UserRole.Attendant);
        var input = new ApproveServiceOrderInput(Guid.NewGuid(), serviceOrder.Id, attendantId);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var updated = result.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
        updated.Status.Should().Be(ServiceOrderStatus.InProgress.ToString());

        using var scope = fixture.Services.CreateScope();

        var serviceOrderRepository = scope.ServiceProvider.GetRequiredService<IServiceOrderRepository>();
        var foundOrder = await serviceOrderRepository.GetByIdAsync(serviceOrder.Id, CancellationToken.None);
        foundOrder.Should().NotBeNull();
        foundOrder!.Status.Should().Be(ServiceOrderStatus.InProgress);
        foundOrder.StatusHistory.Should().HaveCount(3);

        var inventoryItemRepository = scope.ServiceProvider.GetRequiredService<IInventoryItemRepository>();
        var foundInventoryItem = await inventoryItemRepository.GetByIdAsync(inventoryItem.Id, CancellationToken.None);
        foundInventoryItem.Should().NotBeNull();
        foundInventoryItem!.QuantityOnHand.Should().Be(17);
        foundInventoryItem.ReservedQuantity.Should().Be(0);
    }

    [Fact(DisplayName = "ApproveServiceOrderUseCase >> Should fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var attendantId = await CreateUserAsync(UserRole.Attendant);
        var input = new ApproveServiceOrderInput(Guid.NewGuid(), Guid.NewGuid(), attendantId);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service order");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "ApproveServiceOrderUseCase >> Should throw >> When service order is not AwaitingApproval")]
    public async Task Handle_ShouldThrow_WhenServiceOrderIsNotAwaitingApproval()
    {
        // Arrange — a freshly created service order starts in Received, not AwaitingApproval.
        using var scope = fixture.Services.CreateScope();
        var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var customerResult = await createCustomerUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
        var customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var vehicleResult = await createVehicleUseCase.Handle(CreateVehicleInput(customerId), CancellationToken.None);
        var vehicleId = vehicleResult.Result.Should().BeOfType<VehicleResponse>().Subject.Id;

        var mechanicId = await CreateUserAsync(UserRole.Mechanic);

        var createServiceOrderUseCase = scope.ServiceProvider.GetRequiredService<ICreateServiceOrderUseCase>();
        var createInput = new CreateServiceOrderInput(Guid.NewGuid(), customerId, vehicleId, mechanicId, "Engine noise", 12000);
        var createResult = await createServiceOrderUseCase.Handle(createInput, CancellationToken.None);
        var created = createResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;

        var attendantId = await CreateUserAsync(UserRole.Attendant);
        var input = new ApproveServiceOrderInput(Guid.NewGuid(), created.Id, attendantId);

        // Act
        var act = async () => await HandleAsync(input);

        // Assert
        await act.Should().ThrowAsync<Fiap.Workshop.Domain.Abstractions.DomainException>();
    }
}
