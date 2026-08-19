using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.Customers.UpdateCustomer.Boundaries;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class UpdateCustomerUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private sealed record CreatedCustomer(Guid Id, string Name, string Email, string Phone);

    private static CreateCustomerInput CreateCustomerInput() => new(
        Guid.NewGuid(),
        "Integration Customer",
        TestData.Document(),
        TestData.Email(),
        TestData.ShortString(15)
    );

    private async Task<CreatedCustomer> CreateCustomerAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var input = CreateCustomerInput();
        var result = await createUseCase.Handle(input, CancellationToken.None);
        var response = result.Result.Should().BeOfType<CustomerResponse>().Subject;
        return new CreatedCustomer(response.Id, response.Name, input.Email, input.Phone);
    }

    private async Task<Output> HandleAsync(UpdateCustomerInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateCustomerUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "UpdateCustomerUseCase >> Should persist changes >> When customer exists")]
    public async Task Handle_ShouldPersistChanges_WhenCustomerExists()
    {
        // Arrange
        var created = await CreateCustomerAsync();
        var newEmail = TestData.Email();
        var newPhone = TestData.ShortString(15);
        var input = new UpdateCustomerInput(Guid.NewGuid(), created.Id, "Updated Name", newEmail, newPhone);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var updated = result.Result.Should().BeOfType<CustomerResponse>().Subject;
        updated.Id.Should().Be(created.Id);
        updated.Name.Should().Be("Updated Name");
        updated.Phone.Should().Be(newPhone);

        using var scope = fixture.Services.CreateScope();
        var customerRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var found = await customerRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Updated Name");
        found.Email.Should().Be(newEmail);
        found.Phone.Should().Be(newPhone);
    }

    [Fact(DisplayName = "UpdateCustomerUseCase >> Should fail >> When customer does not exist")]
    public async Task Handle_ShouldFail_WhenCustomerDoesNotExist()
    {
        // Arrange
        var input = new UpdateCustomerInput(Guid.NewGuid(), Guid.NewGuid(), "Ghost Customer", TestData.Email(), TestData.ShortString(15));

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find customer");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "UpdateCustomerUseCase >> Should fail >> When new email already belongs to another customer")]
    public async Task Handle_ShouldFail_WhenNewEmailAlreadyBelongsToAnotherCustomer()
    {
        // Arrange
        var other = await CreateCustomerAsync();
        var created = await CreateCustomerAsync();
        var input = new UpdateCustomerInput(Guid.NewGuid(), created.Id, created.Name, other.Email, created.Phone);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle()
            .Which.Should().Be($"Customer with email {other.Email} already exists.");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "UpdateCustomerUseCase >> Should fail >> When new phone already belongs to another customer")]
    public async Task Handle_ShouldFail_WhenNewPhoneAlreadyBelongsToAnotherCustomer()
    {
        // Arrange
        var other = await CreateCustomerAsync();
        var created = await CreateCustomerAsync();
        var input = new UpdateCustomerInput(Guid.NewGuid(), created.Id, created.Name, created.Email + ".br", other.Phone);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle()
            .Which.Should().Be($"Customer with phone {other.Phone} already exists.");
        result.Result.Should().BeNull();
    }
}
