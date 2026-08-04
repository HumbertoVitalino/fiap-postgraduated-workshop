using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Infrastructure.Repositories;

[Collection("Integration")]
public sealed class CustomerRepositoryTests(DatabaseFixture fixture)
{
    private static Customer CreateCustomer(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        "Integration Customer",
        TestData.ShortString(14),
        TestData.Email(),
        TestData.ShortString(15),
        DateTime.UtcNow,
        DateTime.UtcNow
    );

    private async Task WithScopeAsync(Func<ICustomerRepository, Task> action)
    {
        using var scope = fixture.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<ICustomerRepository>());
    }

    private async Task SeedAsync(Customer customer) => await WithScopeAsync(async repo =>
    {
        await repo.AddAsync(customer, CancellationToken.None);
        await repo.UnitOfWork.CommitAsync(CancellationToken.None);
    });

    [Fact(DisplayName = "CustomerRepository >> Should persist and retrieve >> When adding a new customer")]
    public async Task CustomerRepository_ShouldPersistAndRetrieve_WhenAddingNewCustomer()
    {
        // Arrange
        var customer = CreateCustomer();

        // Act
        await WithScopeAsync(async repo =>
        {
            await repo.AddAsync(customer, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        Customer? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(customer.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(customer.Id, found!.Id);
        Assert.Equal(customer.Name, found.Name);
        Assert.Equal(customer.Document, found.Document);
        Assert.Equal(customer.Email, found.Email);
        Assert.Equal(customer.Phone, found.Phone);
    }

    [Fact(DisplayName = "CustomerRepository >> Should return null >> When customer does not exist")]
    public async Task CustomerRepository_ShouldReturnNull_WhenCustomerDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        Customer? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(id, CancellationToken.None));

        // Assert
        Assert.Null(found);
    }

    [Fact(DisplayName = "CustomerRepository >> Should persist changes >> When updating an existing customer")]
    public async Task CustomerRepository_ShouldPersistChanges_WhenUpdatingExistingCustomer()
    {
        // Arrange
        var customer = CreateCustomer();
        await SeedAsync(customer);

        var updated = new Customer(
            customer.Id, "Updated Name", customer.Document, customer.Email, customer.Phone,
            customer.CreatedAt, DateTime.UtcNow);

        // Act
        await WithScopeAsync(async repo =>
        {
            repo.Update(updated);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        Customer? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(customer.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal("Updated Name", found!.Name);
    }

    [Fact(DisplayName = "CustomerRepository >> Should remove entity >> When removing an existing customer")]
    public async Task CustomerRepository_ShouldRemoveEntity_WhenRemovingExistingCustomer()
    {
        // Arrange
        var customer = CreateCustomer();
        await SeedAsync(customer);

        // Act
        await WithScopeAsync(async repo =>
        {
            repo.Remove(customer);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        Customer? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(customer.Id, CancellationToken.None));

        Assert.Null(found);
    }
}
