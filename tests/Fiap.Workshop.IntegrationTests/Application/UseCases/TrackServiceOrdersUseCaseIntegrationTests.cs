using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.TrackServiceOrders.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class TrackServiceOrdersUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateCustomerInput CreateCustomerInput(string document) => new(
        Guid.NewGuid(),
        "Tracking Owner",
        document,
        TestData.Email(),
        TestData.ShortString(15)
    );

    private static CreateVehicleInput CreateVehicleInput(Guid customerId, string licensePlate) => new(
        Guid.NewGuid(),
        customerId,
        licensePlate,
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
        var userRepository = scope.ServiceProvider.GetRequiredService<Fiap.Workshop.Application.Interfaces.Repositories.IUserRepository>();
        await userRepository.AddAsync(user, CancellationToken.None);
        await userRepository.UnitOfWork.CommitAsync(CancellationToken.None);

        return user.Id;
    }

    private async Task<(string Document, string LicensePlate, ServiceOrderResponse ServiceOrder)> CreateServiceOrderForNewVehicleAsync()
    {
        var document = TestData.Document();
        var licensePlate = TestData.LicensePlate();

        using var scope = fixture.Services.CreateScope();

        var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var customerResult = await createCustomerUseCase.Handle(CreateCustomerInput(document), CancellationToken.None);
        var customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var vehicleResult = await createVehicleUseCase.Handle(CreateVehicleInput(customerId, licensePlate), CancellationToken.None);
        var vehicleId = vehicleResult.Result.Should().BeOfType<VehicleResponse>().Subject.Id;

        var mechanicId = await CreateUserAsync(UserRole.Mechanic);

        var createServiceOrderUseCase = scope.ServiceProvider.GetRequiredService<ICreateServiceOrderUseCase>();
        var createInput = new CreateServiceOrderInput(Guid.NewGuid(), customerId, vehicleId, mechanicId, "Engine noise", 12000);
        var createResult = await createServiceOrderUseCase.Handle(createInput, CancellationToken.None);
        var created = createResult.Result.Should().BeOfType<ServiceOrderResponse>().Subject;

        return (document, licensePlate, created);
    }

    private async Task<Output> HandleAsync(TrackServiceOrdersInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ITrackServiceOrdersUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "TrackServiceOrdersUseCase >> Should return the vehicle's orders >> When plate and document match")]
    public async Task Handle_ShouldReturnVehicleOrders_WhenPlateAndDocumentMatch()
    {
        // Arrange
        var (document, licensePlate, serviceOrder) = await CreateServiceOrderForNewVehicleAsync();
        var input = new TrackServiceOrdersInput(Guid.NewGuid(), document, licensePlate);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var orders = result.Result.Should().BeAssignableTo<IReadOnlyCollection<ServiceOrderTrackingResponse>>().Subject;
        orders.Should().ContainSingle().Which.Id.Should().Be(serviceOrder.Id);
    }

    [Fact(DisplayName = "TrackServiceOrdersUseCase >> Should return an empty list >> When license plate does not exist")]
    public async Task Handle_ShouldReturnEmptyList_WhenLicensePlateDoesNotExist()
    {
        // Arrange
        var input = new TrackServiceOrdersInput(Guid.NewGuid(), TestData.Document(), TestData.LicensePlate());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeAssignableTo<IReadOnlyCollection<ServiceOrderTrackingResponse>>().Which.Should().BeEmpty();
    }

    [Fact(DisplayName = "TrackServiceOrdersUseCase >> Should return an empty list >> When document does not match the vehicle's owner")]
    public async Task Handle_ShouldReturnEmptyList_WhenDocumentDoesNotMatchVehicleOwner()
    {
        // Arrange — plate is real, but the document informed belongs to nobody in the system.
        var (_, licensePlate, _) = await CreateServiceOrderForNewVehicleAsync();
        var input = new TrackServiceOrdersInput(Guid.NewGuid(), TestData.Document(), licensePlate);

        // Act
        var result = await HandleAsync(input);

        // Assert — indistinguishable from "plate doesn't exist", by design (no existence leak).
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeAssignableTo<IReadOnlyCollection<ServiceOrderTrackingResponse>>().Which.Should().BeEmpty();
    }
}
