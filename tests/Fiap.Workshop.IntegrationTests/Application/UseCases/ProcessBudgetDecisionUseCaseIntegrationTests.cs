using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ProcessBudgetDecision;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ProcessBudgetDecision.Boundaries;
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
public sealed class ProcessBudgetDecisionUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<ServiceOrderResponse> CreateReceivedServiceOrderAsync(Guid mechanicId)
    {
        using var scope = fixture.Services.CreateScope();

        var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var customerResult = await createCustomerUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
        var customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var vehicleResult = await createVehicleUseCase.Handle(CreateVehicleInput(customerId), CancellationToken.None);
        var vehicleId = vehicleResult.Result.Should().BeOfType<VehicleResponse>().Subject.Id;

        var createServiceOrderUseCase = scope.ServiceProvider.GetRequiredService<ICreateServiceOrderUseCase>();
        var createInput = new CreateServiceOrderInput(Guid.NewGuid(), customerId, vehicleId, mechanicId, "Engine noise", 12000);
        var createResult = await createServiceOrderUseCase.Handle(createInput, CancellationToken.None);
        return createResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
    }

    private async Task<ServiceOrderResponse> CreateAwaitingApprovalServiceOrderAsync(InventoryItem inventoryItem, int quantity)
    {
        var mechanicId = await CreateUserAsync(UserRole.Mechanic);
        var created = await CreateReceivedServiceOrderAsync(mechanicId);

        using var scope = fixture.Services.CreateScope();

        var startDiagnosisUseCase = scope.ServiceProvider.GetRequiredService<IStartDiagnosisUseCase>();
        var diagnosisInput = new StartDiagnosisInput(Guid.NewGuid(), created.Id, mechanicId, "Worn brake pads");
        await startDiagnosisUseCase.Handle(diagnosisInput, CancellationToken.None);

        var addBudgetUseCase = scope.ServiceProvider.GetRequiredService<IAddBudgetUseCase>();
        var budgetInput = new AddBudgetInput(
            Guid.NewGuid(), created.Id, mechanicId, [], [new AddBudgetPartItem(inventoryItem.Id, quantity)]);
        var budgetResult = await addBudgetUseCase.Handle(budgetInput, CancellationToken.None);
        return budgetResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
    }

    private async Task<Output> HandleAsync(ProcessBudgetDecisionInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IProcessBudgetDecisionUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    private async Task<(ServiceOrder ServiceOrder, InventoryItem InventoryItem)> FindAsync(Guid serviceOrderId, Guid inventoryItemId)
    {
        using var scope = fixture.Services.CreateScope();

        var serviceOrderRepository = scope.ServiceProvider.GetRequiredService<IServiceOrderRepository>();
        var foundOrder = await serviceOrderRepository.GetByIdAsync(serviceOrderId, CancellationToken.None);
        foundOrder.Should().NotBeNull();

        var inventoryItemRepository = scope.ServiceProvider.GetRequiredService<IInventoryItemRepository>();
        var foundInventoryItem = await inventoryItemRepository.GetByIdAsync(inventoryItemId, CancellationToken.None);
        foundInventoryItem.Should().NotBeNull();

        return (foundOrder!, foundInventoryItem!);
    }

    [Fact(DisplayName = "ProcessBudgetDecisionUseCase >> Should commit stock and move to InProgress >> When decision is Approved")]
    public async Task Handle_ShouldCommitStockAndMoveToInProgress_WhenDecisionIsApproved()
    {
        // Arrange
        var inventoryItem = await CreateInventoryItemAsync(20);
        var serviceOrder = await CreateAwaitingApprovalServiceOrderAsync(inventoryItem, 3);
        var input = new ProcessBudgetDecisionInput(Guid.NewGuid(), serviceOrder.Id, BudgetDecision.Approved);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeOfType<ServiceOrderResponse>().Subject
            .Status.Should().Be(ServiceOrderStatus.InProgress.ToString());

        var (foundOrder, foundInventoryItem) = await FindAsync(serviceOrder.Id, inventoryItem.Id);
        foundOrder.Status.Should().Be(ServiceOrderStatus.InProgress);
        foundOrder.StatusHistory.Should().HaveCount(3);
        foundOrder.StatusHistory.Should().ContainSingle(h => h.CurrentStatus == ServiceOrderStatus.InProgress)
            .Which.ChangedBy.Should().Be(ProcessBudgetDecisionUseCase.SystemUserId);
        foundInventoryItem.QuantityOnHand.Should().Be(17);
        foundInventoryItem.ReservedQuantity.Should().Be(0);
    }

    [Fact(DisplayName = "ProcessBudgetDecisionUseCase >> Should release stock and move to Cancelled >> When decision is Rejected")]
    public async Task Handle_ShouldReleaseStockAndMoveToCancelled_WhenDecisionIsRejected()
    {
        // Arrange
        var inventoryItem = await CreateInventoryItemAsync(20);
        var serviceOrder = await CreateAwaitingApprovalServiceOrderAsync(inventoryItem, 3);
        var input = new ProcessBudgetDecisionInput(Guid.NewGuid(), serviceOrder.Id, BudgetDecision.Rejected);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeOfType<ServiceOrderResponse>().Subject
            .Status.Should().Be(ServiceOrderStatus.Cancelled.ToString());

        var (foundOrder, foundInventoryItem) = await FindAsync(serviceOrder.Id, inventoryItem.Id);
        foundOrder.Status.Should().Be(ServiceOrderStatus.Cancelled);
        foundOrder.StatusHistory.Should().HaveCount(3);
        foundOrder.StatusHistory.Should().ContainSingle(h => h.CurrentStatus == ServiceOrderStatus.Cancelled)
            .Which.ChangedBy.Should().Be(ProcessBudgetDecisionUseCase.SystemUserId);
        foundInventoryItem.QuantityOnHand.Should().Be(20);
        foundInventoryItem.ReservedQuantity.Should().Be(0);
    }

    [Theory(DisplayName = "ProcessBudgetDecisionUseCase >> Should not change stock nor history >> When the same decision is sent twice")]
    [InlineData(BudgetDecision.Approved, 17)]
    [InlineData(BudgetDecision.Rejected, 20)]
    public async Task Handle_ShouldNotChangeStockNorHistory_WhenTheSameDecisionIsSentTwice(BudgetDecision decision, int expectedQuantityOnHand)
    {
        // Arrange
        var inventoryItem = await CreateInventoryItemAsync(20);
        var serviceOrder = await CreateAwaitingApprovalServiceOrderAsync(inventoryItem, 3);
        await HandleAsync(new ProcessBudgetDecisionInput(Guid.NewGuid(), serviceOrder.Id, decision));
        var input = new ProcessBudgetDecisionInput(Guid.NewGuid(), serviceOrder.Id, decision);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Messages.Should().ContainSingle().Which.Should().Be("Budget decision already applied");

        var (foundOrder, foundInventoryItem) = await FindAsync(serviceOrder.Id, inventoryItem.Id);
        foundOrder.StatusHistory.Should().HaveCount(3);
        foundInventoryItem.QuantityOnHand.Should().Be(expectedQuantityOnHand);
        foundInventoryItem.ReservedQuantity.Should().Be(0);
    }

    [Fact(DisplayName = "ProcessBudgetDecisionUseCase >> Should throw >> When the opposite decision was already applied")]
    public async Task Handle_ShouldThrow_WhenTheOppositeDecisionWasAlreadyApplied()
    {
        // Arrange
        var inventoryItem = await CreateInventoryItemAsync(20);
        var serviceOrder = await CreateAwaitingApprovalServiceOrderAsync(inventoryItem, 3);
        await HandleAsync(new ProcessBudgetDecisionInput(Guid.NewGuid(), serviceOrder.Id, BudgetDecision.Rejected));
        var input = new ProcessBudgetDecisionInput(Guid.NewGuid(), serviceOrder.Id, BudgetDecision.Approved);

        // Act
        var act = async () => await HandleAsync(input);

        // Assert
        await act.Should().ThrowAsync<Fiap.Workshop.Domain.Abstractions.DomainException>();

        var (foundOrder, foundInventoryItem) = await FindAsync(serviceOrder.Id, inventoryItem.Id);
        foundOrder.Status.Should().Be(ServiceOrderStatus.Cancelled);
        foundInventoryItem.QuantityOnHand.Should().Be(20);
    }

    [Fact(DisplayName = "ProcessBudgetDecisionUseCase >> Should throw >> When service order is not AwaitingApproval")]
    public async Task Handle_ShouldThrow_WhenServiceOrderIsNotAwaitingApproval()
    {
        // Arrange — a freshly created service order starts in Received, not AwaitingApproval.
        var mechanicId = await CreateUserAsync(UserRole.Mechanic);
        var created = await CreateReceivedServiceOrderAsync(mechanicId);
        var input = new ProcessBudgetDecisionInput(Guid.NewGuid(), created.Id, BudgetDecision.Approved);

        // Act
        var act = async () => await HandleAsync(input);

        // Assert
        await act.Should().ThrowAsync<Fiap.Workshop.Domain.Abstractions.DomainException>();
    }

    [Fact(DisplayName = "ProcessBudgetDecisionUseCase >> Should fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var input = new ProcessBudgetDecisionInput(Guid.NewGuid(), Guid.NewGuid(), BudgetDecision.Approved);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service order");
        result.Result.Should().BeNull();
    }
}
