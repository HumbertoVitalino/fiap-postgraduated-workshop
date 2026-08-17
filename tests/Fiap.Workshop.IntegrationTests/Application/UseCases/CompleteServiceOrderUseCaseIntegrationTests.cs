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
public sealed class CompleteServiceOrderUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<(ServiceOrderResponse ServiceOrder, Guid MechanicId)> CreateInProgressServiceOrderAsync(Service service)
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
            Guid.NewGuid(), created.Id, mechanicId, [new AddBudgetServiceItem(service.Id, 1)], []);
        var budgetResult = await addBudgetUseCase.Handle(budgetInput, CancellationToken.None);
        var withBudget = budgetResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;

        var attendantId = await CreateUserAsync(UserRole.Attendant);
        var approveServiceOrderUseCase = scope.ServiceProvider.GetRequiredService<IApproveServiceOrderUseCase>();
        var approveInput = new ApproveServiceOrderInput(Guid.NewGuid(), withBudget.Id, attendantId);
        var approveResult = await approveServiceOrderUseCase.Handle(approveInput, CancellationToken.None);
        var inProgress = approveResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;

        return (inProgress, mechanicId);
    }

    private async Task<Output> HandleAsync(CompleteServiceOrderInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICompleteServiceOrderUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "CompleteServiceOrderUseCase >> Should complete and update the catalog average >> When service order is InProgress and every duration is informed")]
    public async Task Handle_ShouldCompleteAndUpdateTheCatalogAverage_WhenServiceOrderIsInProgressAndEveryDurationIsInformed()
    {
        // Arrange
        var service = await CreateServiceAsync();
        var (serviceOrder, mechanicId) = await CreateInProgressServiceOrderAsync(service);
        var orderServiceId = serviceOrder.Services.Single().Id;

        var input = new CompleteServiceOrderInput(
            Guid.NewGuid(), serviceOrder.Id, mechanicId, [new CompleteServiceOrderDuration(orderServiceId, 45)]);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var updated = result.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
        updated.Status.Should().Be(ServiceOrderStatus.Completed.ToString());
        updated.Services.Single().ActualDuration.Should().Be(45);

        using var scope = fixture.Services.CreateScope();

        var serviceOrderRepository = scope.ServiceProvider.GetRequiredService<IServiceOrderRepository>();
        var foundOrder = await serviceOrderRepository.GetByIdAsync(serviceOrder.Id, CancellationToken.None);
        foundOrder.Should().NotBeNull();
        foundOrder!.Status.Should().Be(ServiceOrderStatus.Completed);
        foundOrder.StatusHistory.Should().HaveCount(4);

        var serviceRepository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var foundService = await serviceRepository.GetByIdAsync(service.Id, CancellationToken.None);
        foundService.Should().NotBeNull();
        foundService!.EstimatedDuration.Should().Be(45);
        foundService.ExecutionCount.Should().Be(1);
    }

    [Fact(DisplayName = "CompleteServiceOrderUseCase >> Should fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var input = new CompleteServiceOrderInput(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), []);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service order");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "CompleteServiceOrderUseCase >> Should throw >> When a service duration is missing")]
    public async Task Handle_ShouldThrow_WhenAServiceDurationIsMissing()
    {
        // Arrange
        var service = await CreateServiceAsync();
        var (serviceOrder, mechanicId) = await CreateInProgressServiceOrderAsync(service);
        var input = new CompleteServiceOrderInput(Guid.NewGuid(), serviceOrder.Id, mechanicId, []);

        // Act
        var act = async () => await HandleAsync(input);

        // Assert
        await act.Should().ThrowAsync<Fiap.Workshop.Domain.Abstractions.DomainException>();
    }

    [Fact(DisplayName = "CompleteServiceOrderUseCase >> Should throw >> When service order is not InProgress")]
    public async Task Handle_ShouldThrow_WhenServiceOrderIsNotInProgress()
    {
        // Arrange — a freshly created service order starts in Received, not InProgress.
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

        var input = new CompleteServiceOrderInput(Guid.NewGuid(), created.Id, mechanicId, []);

        // Act
        var act = async () => await HandleAsync(input);

        // Assert
        await act.Should().ThrowAsync<Fiap.Workshop.Domain.Abstractions.DomainException>();
    }
}
