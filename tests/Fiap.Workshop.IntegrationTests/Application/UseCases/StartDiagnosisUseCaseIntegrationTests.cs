using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
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
public sealed class StartDiagnosisUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<Output> HandleAsync(StartDiagnosisInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IStartDiagnosisUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "StartDiagnosisUseCase >> Should persist status and history >> When service order is Received")]
    public async Task Handle_ShouldPersistStatusAndHistory_WhenServiceOrderIsReceived()
    {
        // Arrange
        var created = await CreateServiceOrderAsync();
        var changedBy = await CreateUserAsync();
        var input = new StartDiagnosisInput(Guid.NewGuid(), created.Id, changedBy, "Worn brake pads");

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var updated = result.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
        updated.Status.Should().Be(ServiceOrderStatus.Diagnosing.ToString());
        updated.DiagnoseDescription.Should().Be("Worn brake pads");

        using var scope = fixture.Services.CreateScope();
        var serviceOrderRepository = scope.ServiceProvider.GetRequiredService<IServiceOrderRepository>();
        var found = await serviceOrderRepository.GetByIdAsync(created.Id, CancellationToken.None);

        found.Should().NotBeNull();
        found!.Status.Should().Be(ServiceOrderStatus.Diagnosing);
        found.DiagnoseDescription.Should().Be("Worn brake pads");

        var history = found.StatusHistory.Should().ContainSingle().Subject;
        history.PreviousStatus.Should().Be(ServiceOrderStatus.Received);
        history.CurrentStatus.Should().Be(ServiceOrderStatus.Diagnosing);
        history.ChangedBy.Should().Be(changedBy);
    }

    [Fact(DisplayName = "StartDiagnosisUseCase >> Should fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var input = new StartDiagnosisInput(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Worn brake pads");

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service order");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "StartDiagnosisUseCase >> Should throw >> When service order is not Received")]
    public async Task Handle_ShouldThrow_WhenServiceOrderIsNotReceived()
    {
        // Arrange
        var created = await CreateServiceOrderAsync();
        var changedBy = await CreateUserAsync();
        var input = new StartDiagnosisInput(Guid.NewGuid(), created.Id, changedBy, "Worn brake pads");
        await HandleAsync(input);

        var secondInput = new StartDiagnosisInput(Guid.NewGuid(), created.Id, changedBy, "Worn brake pads again");

        // Act
        var act = async () => await HandleAsync(secondInput);

        // Assert
        await act.Should().ThrowAsync<Fiap.Workshop.Domain.Abstractions.DomainException>();
    }
}
