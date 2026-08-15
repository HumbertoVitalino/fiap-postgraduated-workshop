using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class CreateCustomerUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateCustomerInput CreateInput(string? document = null) => new(
        Guid.NewGuid(),
        "Integration Customer",
        document ?? TestData.Document(),
        TestData.Email(),
        TestData.ShortString(15)
    );

    private async Task<Output> HandleAsync(CreateCustomerInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "CreateCustomerUseCase >> Should persist customer >> When customer is valid and does not exist")]
    public async Task Handle_ShouldPersistCustomer_WhenCustomerIsValidAndDoesNotExist()
    {
        // Arrange
        var input = CreateInput();

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var created = result.Result.Should().BeOfType<CustomerResponse>().Subject;
        created.Name.Should().Be(input.Name);
        created.Phone.Should().Be(input.Phone);

        using var scope = fixture.Services.CreateScope();
        var customerRepository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var found = await customerRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Document.Should().Be(input.Document);
        found.Email.Should().Be(input.Email);
    }

    [Fact(DisplayName = "CreateCustomerUseCase >> Should fail >> When customer already exists")]
    public async Task Handle_ShouldFail_WhenCustomerAlreadyExists()
    {
        // Arrange
        var input = CreateInput();
        await HandleAsync(input);

        var duplicateInput = CreateInput(input.Document);

        // Act
        var result = await HandleAsync(duplicateInput);

        // Assert
        result.ErrorMessages.Should().ContainSingle()
            .Which.Should().Be("Customer with the provided document already exists.");
        result.Result.Should().BeNull();
    }
}
