using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ApproveServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CompleteServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.DeliverServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.RejectServiceOrder.Boundaries;
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
public sealed class DeliverServiceOrderUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<ServiceOrderResponse> CreateAwaitingApprovalServiceOrderAsync()
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
        var budgetInput = new AddBudgetInput(Guid.NewGuid(), created.Id, mechanicId, [], []);
        var budgetResult = await addBudgetUseCase.Handle(budgetInput, CancellationToken.None);
        return budgetResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
    }

    private async Task<ServiceOrderResponse> CreateCompletedServiceOrderAsync()
    {
        var awaitingApproval = await CreateAwaitingApprovalServiceOrderAsync();
        var attendantId = await CreateUserAsync(UserRole.Attendant);

        using var scope = fixture.Services.CreateScope();

        var approveUseCase = scope.ServiceProvider.GetRequiredService<IApproveServiceOrderUseCase>();
        var approveInput = new ApproveServiceOrderInput(Guid.NewGuid(), awaitingApproval.Id, attendantId);
        var approveResult = await approveUseCase.Handle(approveInput, CancellationToken.None);
        var inProgress = approveResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;

        var completeUseCase = scope.ServiceProvider.GetRequiredService<ICompleteServiceOrderUseCase>();
        var completeInput = new CompleteServiceOrderInput(Guid.NewGuid(), inProgress.Id, inProgress.CreatedBy, []);
        var completeResult = await completeUseCase.Handle(completeInput, CancellationToken.None);
        return completeResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
    }

    private async Task<ServiceOrderResponse> CreateCancelledServiceOrderAsync()
    {
        var awaitingApproval = await CreateAwaitingApprovalServiceOrderAsync();
        var attendantId = await CreateUserAsync(UserRole.Attendant);

        using var scope = fixture.Services.CreateScope();

        var rejectUseCase = scope.ServiceProvider.GetRequiredService<IRejectServiceOrderUseCase>();
        var rejectInput = new RejectServiceOrderInput(Guid.NewGuid(), awaitingApproval.Id, attendantId);
        var rejectResult = await rejectUseCase.Handle(rejectInput, CancellationToken.None);
        return rejectResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
    }

    private async Task<Output> HandleAsync(DeliverServiceOrderInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IDeliverServiceOrderUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "DeliverServiceOrderUseCase >> Should close the order >> When service order is Completed")]
    public async Task Handle_ShouldCloseTheOrder_WhenServiceOrderIsCompleted()
    {
        // Arrange
        var serviceOrder = await CreateCompletedServiceOrderAsync();
        var attendantId = await CreateUserAsync(UserRole.Attendant);
        var input = new DeliverServiceOrderInput(Guid.NewGuid(), serviceOrder.Id, attendantId);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var updated = result.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
        updated.Status.Should().Be(ServiceOrderStatus.Delivered.ToString());
        updated.ClosedAt.Should().NotBeNull();

        using var scope = fixture.Services.CreateScope();
        var serviceOrderRepository = scope.ServiceProvider.GetRequiredService<IServiceOrderRepository>();
        var foundOrder = await serviceOrderRepository.GetByIdAsync(serviceOrder.Id, CancellationToken.None);
        foundOrder.Should().NotBeNull();
        foundOrder!.Status.Should().Be(ServiceOrderStatus.Delivered);
        foundOrder.ClosedAt.Should().NotBeNull();
        foundOrder.StatusHistory.Should().HaveCount(5);
    }

    [Fact(DisplayName = "DeliverServiceOrderUseCase >> Should close the order >> When service order is Cancelled")]
    public async Task Handle_ShouldCloseTheOrder_WhenServiceOrderIsCancelled()
    {
        // Arrange — customer rejected the budget but still needs to pick up the vehicle from the shop.
        var serviceOrder = await CreateCancelledServiceOrderAsync();
        var attendantId = await CreateUserAsync(UserRole.Attendant);
        var input = new DeliverServiceOrderInput(Guid.NewGuid(), serviceOrder.Id, attendantId);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var updated = result.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
        updated.Status.Should().Be(ServiceOrderStatus.Delivered.ToString());
        updated.ClosedAt.Should().NotBeNull();

        using var scope = fixture.Services.CreateScope();
        var serviceOrderRepository = scope.ServiceProvider.GetRequiredService<IServiceOrderRepository>();
        var foundOrder = await serviceOrderRepository.GetByIdAsync(serviceOrder.Id, CancellationToken.None);
        foundOrder.Should().NotBeNull();
        foundOrder!.Status.Should().Be(ServiceOrderStatus.Delivered);
        foundOrder.ClosedAt.Should().NotBeNull();
        foundOrder.StatusHistory.Should().HaveCount(4);
    }

    [Fact(DisplayName = "DeliverServiceOrderUseCase >> Should fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var attendantId = await CreateUserAsync(UserRole.Attendant);
        var input = new DeliverServiceOrderInput(Guid.NewGuid(), Guid.NewGuid(), attendantId);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service order");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "DeliverServiceOrderUseCase >> Should throw >> When service order is neither Completed nor Cancelled")]
    public async Task Handle_ShouldThrow_WhenServiceOrderIsNeitherCompletedNorCancelled()
    {
        // Arrange — freshly opened order starts in Received.
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
        var input = new DeliverServiceOrderInput(Guid.NewGuid(), created.Id, attendantId);

        // Act
        var act = async () => await HandleAsync(input);

        // Assert
        await act.Should().ThrowAsync<Fiap.Workshop.Domain.Abstractions.DomainException>();
    }
}
