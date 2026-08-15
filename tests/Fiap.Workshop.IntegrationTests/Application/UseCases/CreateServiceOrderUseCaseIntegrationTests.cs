using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class CreateServiceOrderUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private static CreateServiceOrderInput CreateServiceOrderInput(Guid customerId, Guid vehicleId, Guid createdBy) => new(
        Guid.NewGuid(),
        customerId,
        vehicleId,
        createdBy,
        "Engine noise",
        12000
    );

    private async Task<Guid> CreateCustomerAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var result = await useCase.Handle(CreateCustomerInput(), CancellationToken.None);
        return result.Result.Should().BeOfType<CustomerResponse>().Subject.Id;
    }

    private async Task<Guid> CreateVehicleAsync(Guid customerId)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var result = await useCase.Handle(CreateVehicleInput(customerId), CancellationToken.None);
        return result.Result.Should().BeOfType<VehicleResponse>().Subject.Id;
    }

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

    private async Task<Output> HandleAsync(CreateServiceOrderInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICreateServiceOrderUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "CreateServiceOrderUseCase >> Should persist service order >> When customer and vehicle exist and vehicle belongs to customer")]
    public async Task Handle_ShouldPersistServiceOrder_WhenCustomerAndVehicleExistAndVehicleBelongsToCustomer()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var vehicleId = await CreateVehicleAsync(customerId);
        var createdBy = await CreateUserAsync();
        var input = CreateServiceOrderInput(customerId, vehicleId, createdBy);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var created = result.Result.Should().BeOfType<ServiceOrderResponse>().Subject;
        created.CustomerId.Should().Be(customerId);
        created.VehicleId.Should().Be(vehicleId);
        created.CreatedBy.Should().Be(createdBy);
        created.Status.Should().Be(ServiceOrderStatus.Received.ToString());
        created.Subtotal.Should().Be(0);
        created.Total.Should().Be(0);

        using var scope = fixture.Services.CreateScope();
        var serviceOrderRepository = scope.ServiceProvider.GetRequiredService<IServiceOrderRepository>();
        var found = await serviceOrderRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.CustomerId.Should().Be(customerId);
        found.VehicleId.Should().Be(vehicleId);
        found.Status.Should().Be(ServiceOrderStatus.Received);
        found.Parts.Should().BeEmpty();
        found.Services.Should().BeEmpty();
    }

    [Fact(DisplayName = "CreateServiceOrderUseCase >> Should fail >> When customer does not exist")]
    public async Task Handle_ShouldFail_WhenCustomerDoesNotExist()
    {
        // Arrange
        var createdBy = await CreateUserAsync();
        var input = CreateServiceOrderInput(Guid.NewGuid(), Guid.NewGuid(), createdBy);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find customer");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "CreateServiceOrderUseCase >> Should fail >> When vehicle does not exist")]
    public async Task Handle_ShouldFail_WhenVehicleDoesNotExist()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var createdBy = await CreateUserAsync();
        var input = CreateServiceOrderInput(customerId, Guid.NewGuid(), createdBy);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find vehicle");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "CreateServiceOrderUseCase >> Should fail >> When vehicle does not belong to customer")]
    public async Task Handle_ShouldFail_WhenVehicleDoesNotBelongToCustomer()
    {
        // Arrange
        var vehicleOwnerId = await CreateCustomerAsync();
        var vehicleId = await CreateVehicleAsync(vehicleOwnerId);
        var otherCustomerId = await CreateCustomerAsync();
        var createdBy = await CreateUserAsync();
        var input = CreateServiceOrderInput(otherCustomerId, vehicleId, createdBy);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Vehicle does not belong to the specified customer");
        result.Result.Should().BeNull();
    }
}
