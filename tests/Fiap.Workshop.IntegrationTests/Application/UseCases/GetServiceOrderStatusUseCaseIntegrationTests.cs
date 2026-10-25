using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrderStatus.Boundaries;
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
public sealed class GetServiceOrderStatusUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<Output> HandleAsync(GetServiceOrderStatusInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetServiceOrderStatusUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "GetServiceOrderStatusUseCase >> Should return Received since opening >> When service order was just opened")]
    public async Task Handle_ShouldReturnReceivedSinceOpening_WhenServiceOrderWasJustOpened()
    {
        // Arrange
        var created = await CreateServiceOrderAsync();

        // Act
        var result = await HandleAsync(new GetServiceOrderStatusInput(Guid.NewGuid(), created.Id));

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var status = result.Result.Should().BeOfType<ServiceOrderStatusResponse>().Subject;
        status.Id.Should().Be(created.Id);
        status.Status.Should().Be(ServiceOrderStatus.Received.ToString());
        status.StatusChangedAt.Should().BeCloseTo(created.OpenedAt, TimeSpan.FromMilliseconds(1));
    }

    [Fact(DisplayName = "GetServiceOrderStatusUseCase >> Should return new status and change date >> When service order changed status")]
    public async Task Handle_ShouldReturnNewStatusAndChangeDate_WhenServiceOrderChangedStatus()
    {
        // Arrange
        var created = await CreateServiceOrderAsync();
        var mechanicId = await CreateUserAsync();

        using (var scope = fixture.Services.CreateScope())
        {
            var startDiagnosisUseCase = scope.ServiceProvider.GetRequiredService<IStartDiagnosisUseCase>();
            var startResult = await startDiagnosisUseCase.Handle(
                new StartDiagnosisInput(Guid.NewGuid(), created.Id, mechanicId, "Worn timing belt"), CancellationToken.None);
            startResult.ErrorMessages.Should().BeEmpty();
        }

        ServiceOrderStatusHistory diagnosisEntry;
        using (var scope = fixture.Services.CreateScope())
        {
            var serviceOrderRepository = scope.ServiceProvider.GetRequiredService<IServiceOrderRepository>();
            var persisted = await serviceOrderRepository.GetByIdAsync(created.Id, CancellationToken.None);
            diagnosisEntry = persisted!.StatusHistory.Should().ContainSingle().Subject;
        }

        // Act
        var result = await HandleAsync(new GetServiceOrderStatusInput(Guid.NewGuid(), created.Id));

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var status = result.Result.Should().BeOfType<ServiceOrderStatusResponse>().Subject;
        status.Status.Should().Be(ServiceOrderStatus.Diagnosing.ToString());
        status.StatusChangedAt.Should().Be(diagnosisEntry.ChangedAt);
        status.StatusChangedAt.Should().BeOnOrAfter(created.OpenedAt);
    }

    [Fact(DisplayName = "GetServiceOrderStatusUseCase >> Should fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Act
        var result = await HandleAsync(new GetServiceOrderStatusInput(Guid.NewGuid(), Guid.NewGuid()));

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service order");
        result.Result.Should().BeNull();
    }
}
