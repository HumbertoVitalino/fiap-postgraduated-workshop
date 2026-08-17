using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.DeleteVehicle.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class DeleteVehicleUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateCustomerInput CreateCustomerInput() => new(
        Guid.NewGuid(),
        "Integration Customer",
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

    private async Task<(Guid CustomerId, VehicleResponse Vehicle)> CreateVehicleAsync()
    {
        using var scope = fixture.Services.CreateScope();

        var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var customerResult = await createCustomerUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
        var customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var result = await createVehicleUseCase.Handle(CreateVehicleInput(customerId), CancellationToken.None);
        return (customerId, result.Result.Should().BeOfType<VehicleResponse>().Subject);
    }

    private async Task CreateServiceOrderForVehicleAsync(Guid customerId, Guid vehicleId)
    {
        var user = new User(
            Guid.NewGuid(), TestData.Email(), "Attendant", TestData.ShortString(32), UserRole.Attendant,
            DateTime.Now, DateTime.Now);

        using var scope = fixture.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        await userRepository.AddAsync(user, CancellationToken.None);
        await userRepository.UnitOfWork.CommitAsync(CancellationToken.None);

        var createServiceOrderUseCase = scope.ServiceProvider.GetRequiredService<ICreateServiceOrderUseCase>();
        var createInput = new CreateServiceOrderInput(Guid.NewGuid(), customerId, vehicleId, user.Id, "Engine noise", 12000);
        var result = await createServiceOrderUseCase.Handle(createInput, CancellationToken.None);
        result.ErrorMessages.Should().BeEmpty();
    }

    private async Task<Output> HandleAsync(DeleteVehicleInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IDeleteVehicleUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "DeleteVehicleUseCase >> Should remove vehicle >> When vehicle has no service orders")]
    public async Task Handle_ShouldRemoveVehicle_WhenVehicleHasNoServiceOrders()
    {
        // Arrange
        var (_, vehicle) = await CreateVehicleAsync();
        var input = new DeleteVehicleInput(Guid.NewGuid(), vehicle.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();

        using var scope = fixture.Services.CreateScope();
        var vehicleRepository = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
        var found = await vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        found.Should().BeNull();
    }

    [Fact(DisplayName = "DeleteVehicleUseCase >> Should succeed idempotently >> When vehicle does not exist")]
    public async Task Handle_ShouldSucceedIdempotently_WhenVehicleDoesNotExist()
    {
        // Arrange
        var input = new DeleteVehicleInput(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "DeleteVehicleUseCase >> Should fail >> When vehicle has service orders associated")]
    public async Task Handle_ShouldFail_WhenVehicleHasServiceOrdersAssociated()
    {
        // Arrange
        var (customerId, vehicle) = await CreateVehicleAsync();
        await CreateServiceOrderForVehicleAsync(customerId, vehicle.Id);
        var input = new DeleteVehicleInput(Guid.NewGuid(), vehicle.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Vehicle has service orders associated and cannot be deleted.");

        using var scope = fixture.Services.CreateScope();
        var vehicleRepository = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
        var found = await vehicleRepository.GetByIdAsync(vehicle.Id, CancellationToken.None);
        found.Should().NotBeNull();
    }
}
