using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class CreateVehicleUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateCustomerInput CreateCustomerInput() => new(
        Guid.NewGuid(),
        "Vehicle Owner",
        TestData.Document(),
        TestData.Email(),
        TestData.ShortString(15)
    );

    private static CreateVehicleInput CreateVehicleInput(Guid customerId, string? licensePlate = null) => new(
        Guid.NewGuid(),
        customerId,
        licensePlate ?? TestData.LicensePlate(),
        "Ford",
        "Ka",
        2020,
        2021,
        "Black"
    );

    private async Task<Guid> CreateCustomerAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var result = await useCase.Handle(CreateCustomerInput(), CancellationToken.None);
        return result.Result.Should().BeOfType<CustomerResponse>().Subject.Id;
    }

    private async Task<Output> HandleAsync(CreateVehicleInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "CreateVehicleUseCase >> Should persist vehicle >> When customer exists and plate is not in use")]
    public async Task Handle_ShouldPersistVehicle_WhenCustomerExistsAndPlateIsNotInUse()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var input = CreateVehicleInput(customerId);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var created = result.Result.Should().BeOfType<VehicleResponse>().Subject;
        created.CustomerId.Should().Be(customerId);
        created.LicensePlate.Should().Be(input.LicensePlate);

        using var scope = fixture.Services.CreateScope();
        var vehicleRepository = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
        var found = await vehicleRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.CustomerId.Should().Be(customerId);
        found.ManufactureYear.Should().Be(2020);
        found.ModelYear.Should().Be(2021);
    }

    [Fact(DisplayName = "CreateVehicleUseCase >> Should fail >> When customer does not exist")]
    public async Task Handle_ShouldFail_WhenCustomerDoesNotExist()
    {
        // Arrange
        var input = CreateVehicleInput(Guid.NewGuid());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find customer");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "CreateVehicleUseCase >> Should fail >> When license plate already exists")]
    public async Task Handle_ShouldFail_WhenLicensePlateAlreadyExists()
    {
        // Arrange
        var customerId = await CreateCustomerAsync();
        var firstInput = CreateVehicleInput(customerId);
        await HandleAsync(firstInput);

        var otherCustomerId = await CreateCustomerAsync();
        var duplicateInput = CreateVehicleInput(otherCustomerId, firstInput.LicensePlate);

        // Act
        var result = await HandleAsync(duplicateInput);

        // Assert
        result.ErrorMessages.Should().ContainSingle()
            .Which.Should().Be($"Vehicle with license plate {firstInput.LicensePlate} already exists.");
        result.Result.Should().BeNull();
    }
}
