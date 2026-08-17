using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Boundaries;
using Fiap.Workshop.Application.UseCases.InventoryItems.DeleteInventoryItem.Boundaries;
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
public sealed class DeleteInventoryItemUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateInventoryItemInput CreateInventoryItemInput() => new(
        Guid.NewGuid(),
        TestData.ShortString(20),
        "Brake Pad",
        "Front brake pad set",
        100,
        10,
        49.90m,
        UnitOfMeasure.Piece
    );

    private async Task<InventoryItemResponse> CreateInventoryItemAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateInventoryItemUseCase>();
        var result = await createUseCase.Handle(CreateInventoryItemInput(), CancellationToken.None);
        return result.Result.Should().BeOfType<InventoryItemResponse>().Subject;
    }

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

    private async Task UseInventoryItemInBudgetAsync(InventoryItemResponse inventoryItem)
    {
        using var scope = fixture.Services.CreateScope();

        var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var customerResult = await createCustomerUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
        var customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var vehicleResult = await createVehicleUseCase.Handle(CreateVehicleInput(customerId), CancellationToken.None);
        var vehicleId = vehicleResult.Result.Should().BeOfType<VehicleResponse>().Subject.Id;

        var mechanicId = await CreateUserAsync();

        var createServiceOrderUseCase = scope.ServiceProvider.GetRequiredService<ICreateServiceOrderUseCase>();
        var createInput = new CreateServiceOrderInput(Guid.NewGuid(), customerId, vehicleId, mechanicId, "Engine noise", 12000);
        var createResult = await createServiceOrderUseCase.Handle(createInput, CancellationToken.None);
        var created = createResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;

        var startDiagnosisUseCase = scope.ServiceProvider.GetRequiredService<IStartDiagnosisUseCase>();
        var diagnosisInput = new StartDiagnosisInput(Guid.NewGuid(), created.Id, mechanicId, "Worn brake pads");
        await startDiagnosisUseCase.Handle(diagnosisInput, CancellationToken.None);

        var addBudgetUseCase = scope.ServiceProvider.GetRequiredService<IAddBudgetUseCase>();
        var budgetInput = new AddBudgetInput(Guid.NewGuid(), created.Id, mechanicId, [], [new AddBudgetPartItem(inventoryItem.Id, 1)]);
        var budgetResult = await addBudgetUseCase.Handle(budgetInput, CancellationToken.None);
        budgetResult.ErrorMessages.Should().BeEmpty();
    }

    private async Task<Output> HandleAsync(DeleteInventoryItemInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IDeleteInventoryItemUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "DeleteInventoryItemUseCase >> Should remove inventory item >> When inventory item has never been used")]
    public async Task Handle_ShouldRemoveInventoryItem_WhenInventoryItemHasNeverBeenUsed()
    {
        // Arrange
        var created = await CreateInventoryItemAsync();
        var input = new DeleteInventoryItemInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();

        using var scope = fixture.Services.CreateScope();
        var inventoryItemRepository = scope.ServiceProvider.GetRequiredService<IInventoryItemRepository>();
        var found = await inventoryItemRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().BeNull();
    }

    [Fact(DisplayName = "DeleteInventoryItemUseCase >> Should succeed idempotently >> When inventory item does not exist")]
    public async Task Handle_ShouldSucceedIdempotently_WhenInventoryItemDoesNotExist()
    {
        // Arrange
        var input = new DeleteInventoryItemInput(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "DeleteInventoryItemUseCase >> Should fail >> When inventory item has been used in a service order")]
    public async Task Handle_ShouldFail_WhenInventoryItemHasBeenUsedInServiceOrder()
    {
        // Arrange
        var created = await CreateInventoryItemAsync();
        await UseInventoryItemInBudgetAsync(created);
        var input = new DeleteInventoryItemInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Inventory item has been used in a service order and cannot be deleted.");

        using var scope = fixture.Services.CreateScope();
        var inventoryItemRepository = scope.ServiceProvider.GetRequiredService<IInventoryItemRepository>();
        var found = await inventoryItemRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
    }
}
