using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.StartDiagnosis.Boundaries;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Boundaries;
using Fiap.Workshop.Application.UseCases.Services.DeleteService.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class DeleteServiceUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateServiceInput CreateServiceInput() => new(
        Guid.NewGuid(),
        TestData.ShortString(20),
        "Oil Change",
        "Engine oil and filter replacement",
        150.00m,
        60
    );

    private async Task<ServiceResponse> CreateServiceAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateServiceUseCase>();
        var result = await createUseCase.Handle(CreateServiceInput(), CancellationToken.None);
        return result.Result.Should().BeOfType<ServiceResponse>().Subject;
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

    private async Task UseServiceInBudgetAsync(ServiceResponse service)
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
        var budgetInput = new AddBudgetInput(Guid.NewGuid(), created.Id, mechanicId, [new AddBudgetServiceItem(service.Id, 1)], []);
        var budgetResult = await addBudgetUseCase.Handle(budgetInput, CancellationToken.None);
        budgetResult.ErrorMessages.Should().BeEmpty();
    }

    private async Task<Output> HandleAsync(DeleteServiceInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IDeleteServiceUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "DeleteServiceUseCase >> Should remove service >> When service has never been used")]
    public async Task Handle_ShouldRemoveService_WhenServiceHasNeverBeenUsed()
    {
        // Arrange
        var created = await CreateServiceAsync();
        var input = new DeleteServiceInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();

        using var scope = fixture.Services.CreateScope();
        var serviceRepository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var found = await serviceRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().BeNull();
    }

    [Fact(DisplayName = "DeleteServiceUseCase >> Should succeed idempotently >> When service does not exist")]
    public async Task Handle_ShouldSucceedIdempotently_WhenServiceDoesNotExist()
    {
        // Arrange
        var input = new DeleteServiceInput(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "DeleteServiceUseCase >> Should fail >> When service has been used in a service order")]
    public async Task Handle_ShouldFail_WhenServiceHasBeenUsedInServiceOrder()
    {
        // Arrange
        var created = await CreateServiceAsync();
        await UseServiceInBudgetAsync(created);
        var input = new DeleteServiceInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Service has been used in a service order and cannot be deleted.");

        using var scope = fixture.Services.CreateScope();
        var serviceRepository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var found = await serviceRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
    }
}
