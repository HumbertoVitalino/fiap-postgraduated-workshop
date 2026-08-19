using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Customer;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;
using Fiap.Workshop.Application.UseCases.Customers.GetCustomer.Boundaries;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class GetCustomerUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateCustomerInput CreateCustomerInput() => new(
        Guid.NewGuid(),
        "Integration Customer",
        TestData.Document(),
        TestData.Email(),
        TestData.ShortString(15)
    );

    private async Task<CustomerResponse> CreateCustomerAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateCustomerUseCase>();
        var result = await createUseCase.Handle(CreateCustomerInput(), CancellationToken.None);
        return result.Result.Should().BeOfType<CustomerResponse>().Subject;
    }

    private async Task<Output> HandleAsync(GetCustomerInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetCustomerUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "GetCustomerUseCase >> Should return customer >> When customer exists")]
    public async Task Handle_ShouldReturnCustomer_WhenCustomerExists()
    {
        // Arrange
        var created = await CreateCustomerAsync();
        var input = new GetCustomerInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var found = result.Result.Should().BeOfType<CustomerResponse>().Subject;
        found.Id.Should().Be(created.Id);
        found.Name.Should().Be(created.Name);
        found.Phone.Should().Be(created.Phone);
    }

    [Fact(DisplayName = "GetCustomerUseCase >> Should fail >> When customer does not exist")]
    public async Task Handle_ShouldFail_WhenCustomerDoesNotExist()
    {
        // Arrange
        var input = new GetCustomerInput(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle()
            .Which.Should().Be("Unable to find customer");
        result.Result.Should().BeNull();
    }
}
