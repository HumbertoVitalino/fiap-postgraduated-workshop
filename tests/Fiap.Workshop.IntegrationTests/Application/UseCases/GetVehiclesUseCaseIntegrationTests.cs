using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class GetVehiclesUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<Output> HandleAsync(Guid input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetVehiclesUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "GetVehiclesUseCase >> Should return vehicle >> When vehicles exist")]
    public async Task GetVehiclesUseCase_ShouldReturnVehicles_WhenVehiclesExist()
    {
        // Arrange
        var created = await CreateVehicleAsync();

        // Act
        var result = await HandleAsync(Guid.NewGuid());

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var vehicles = result.Result.Should().BeAssignableTo<IEnumerable<VehicleResponse>>().Subject;
        vehicles.Should().Contain(v => v.Id == created.Id);
    }
}
