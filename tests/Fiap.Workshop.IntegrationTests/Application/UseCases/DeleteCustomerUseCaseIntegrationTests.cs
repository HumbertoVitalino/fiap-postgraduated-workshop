using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.Customers.DeleteCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class DeleteCustomerUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateCustomerInput CreateCustomerInput() => new(
        Guid.NewGuid(),
        "Customer To Delete",
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

    private async Task<CustomerResponse> CreateCustomerAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var result = await createUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
        return result.Result.Should().BeOfType<CustomerResponse>().Subject;
    }

    private async Task CreateVehicleForCustomerAsync(Guid customerId)
    {
        using var scope = fixture.Services.CreateScope();
        var createVehicleUseCase = scope.ServiceProvider.GetRequiredService<ICreateVehicleUseCase>();
        var result = await createVehicleUseCase.Handle(CreateVehicleInput(customerId), CancellationToken.None);
        result.Result.Should().BeOfType<VehicleResponse>();
    }

    private async Task<Output> HandleAsync(DeleteCustomerInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IDeleteCustomerUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "DeleteCustomerUseCase >> Should remove customer >> When customer has no vehicles")]
    public async Task Handle_ShouldRemoveCustomer_WhenCustomerHasNoVehicles()
    {
        // Arrange
        var created = await CreateCustomerAsync();
        var input = new DeleteCustomerInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();

        using var scope = fixture.Services.CreateScope();
        var customerRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var found = await customerRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().BeNull();
    }

    [Fact(DisplayName = "DeleteCustomerUseCase >> Should succeed idempotently >> When customer does not exist")]
    public async Task Handle_ShouldSucceedIdempotently_WhenCustomerDoesNotExist()
    {
        // Arrange
        var input = new DeleteCustomerInput(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "DeleteCustomerUseCase >> Should fail >> When customer has vehicles associated")]
    public async Task Handle_ShouldFail_WhenCustomerHasVehiclesAssociated()
    {
        // Arrange
        var created = await CreateCustomerAsync();
        await CreateVehicleForCustomerAsync(created.Id);
        var input = new DeleteCustomerInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Customer has vehicles associated and cannot be deleted.");

        using var scope = fixture.Services.CreateScope();
        var customerRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var found = await customerRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
    }
}
