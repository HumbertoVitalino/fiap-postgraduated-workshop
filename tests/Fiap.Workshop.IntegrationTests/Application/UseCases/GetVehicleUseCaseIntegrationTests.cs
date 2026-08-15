using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicle.Boundaries;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class GetVehicleUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateCustomerInput CreateCustomerInput() => new(
        Guid.NewGuid(),
        "Vehicle Owner",
        TestData.Document(),
        TestData.Email(),
        TestData.ShortString(15)
    );

    private async Task<VehicleResponse> CreateVehicleAsync()
    {
        using var scope = fixture.Services.CreateScope();

        var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var customerResult = await createCustomerUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
        var customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var vehicleInput = new CreateVehicleInput(Guid.NewGuid(), customerId, TestData.LicensePlate(), "Ford", "Ka", 2020, 2021, "Black");
        var vehicleResult = await createVehicleUseCase.Handle(vehicleInput, CancellationToken.None);
        return vehicleResult.Result.Should().BeOfType<VehicleResponse>().Subject;
    }

    private async Task<Output> HandleAsync(GetVehicleInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetVehicleUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "GetVehicleUseCase >> Should return vehicle >> When vehicle exists")]
    public async Task Handle_ShouldReturnVehicle_WhenVehicleExists()
    {
        // Arrange
        var created = await CreateVehicleAsync();
        var input = new GetVehicleInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var found = result.Result.Should().BeOfType<VehicleResponse>().Subject;
        found.Id.Should().Be(created.Id);
        found.LicensePlate.Should().Be(created.LicensePlate);
    }

    [Fact(DisplayName = "GetVehicleUseCase >> Should fail >> When vehicle does not exist")]
    public async Task Handle_ShouldFail_WhenVehicleDoesNotExist()
    {
        // Arrange
        var input = new GetVehicleInput(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find vehicle");
        result.Result.Should().BeNull();
    }
}
