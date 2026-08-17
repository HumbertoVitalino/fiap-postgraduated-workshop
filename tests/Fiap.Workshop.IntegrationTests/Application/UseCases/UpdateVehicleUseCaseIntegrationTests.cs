using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.UpdateVehicle.Boundaries;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class UpdateVehicleUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<VehicleResponse> CreateVehicleAsync()
    {
        using var scope = fixture.Services.CreateScope();

        var createCustomerUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var customerResult = await createCustomerUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
        var customerId = customerResult.Result.Should().BeOfType<CustomerResponse>().Subject.Id;

        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var result = await createVehicleUseCase.Handle(CreateVehicleInput(customerId), CancellationToken.None);
        return result.Result.Should().BeOfType<VehicleResponse>().Subject;
    }

    private async Task<Output> HandleAsync(UpdateVehicleInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateVehicleUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "UpdateVehicleUseCase >> Should persist changes >> When vehicle exists")]
    public async Task Handle_ShouldPersistChanges_WhenVehicleExists()
    {
        // Arrange
        var created = await CreateVehicleAsync();
        var input = new UpdateVehicleInput(Guid.NewGuid(), created.Id, "Toyota", "Corolla", "White", created.ModelYear + 1);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var updated = result.Result.Should().BeOfType<VehicleResponse>().Subject;
        updated.Id.Should().Be(created.Id);
        updated.Brand.Should().Be("Toyota");
        updated.LicensePlate.Should().Be(created.LicensePlate);

        using var scope = fixture.Services.CreateScope();
        var vehicleRepository = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
        var found = await vehicleRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Brand.Should().Be("Toyota");
        found.Model.Should().Be("Corolla");
        found.Color.Should().Be("White");
        found.ModelYear.Should().Be(created.ModelYear + 1);
        found.ManufactureYear.Should().Be(created.ManufactureYear);
    }

    [Fact(DisplayName = "UpdateVehicleUseCase >> Should fail >> When vehicle does not exist")]
    public async Task Handle_ShouldFail_WhenVehicleDoesNotExist()
    {
        // Arrange
        var input = new UpdateVehicleInput(Guid.NewGuid(), Guid.NewGuid(), "Toyota", "Corolla", "White", 2022);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find vehicle");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "UpdateVehicleUseCase >> Should throw >> When ModelYear is earlier than ManufactureYear")]
    public async Task Handle_ShouldThrow_WhenModelYearIsEarlierThanManufactureYear()
    {
        // Arrange
        var created = await CreateVehicleAsync();
        var input = new UpdateVehicleInput(Guid.NewGuid(), created.Id, "Toyota", "Corolla", "White", created.ManufactureYear - 1);

        // Act
        var act = async () => await HandleAsync(input);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
    }
}
