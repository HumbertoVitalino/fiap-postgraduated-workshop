using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Infrastructure.Repositories;

[Collection("Integration")]
public sealed class ServiceRepositoryTests(DatabaseFixture fixture)
{
    private static Service CreateService(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        TestData.ShortString(20),
        "Integration Service",
        "Integration test service",
        150.00m,
        60,
        true,
        DateTime.UtcNow,
        DateTime.UtcNow
    );

    private async Task WithScopeAsync(Func<IServiceRepository, Task> action)
    {
        using var scope = fixture.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<IServiceRepository>());
    }

    private async Task SeedAsync(Service service) => await WithScopeAsync(async repo =>
    {
        await repo.AddAsync(service, CancellationToken.None);
        await repo.UnitOfWork.CommitAsync(CancellationToken.None);
    });

    [Fact(DisplayName = "ServiceRepository >> Should persist and retrieve >> When adding a new service")]
    public async Task ServiceRepository_ShouldPersistAndRetrieve_WhenAddingNewService()
    {
        // Arrange
        var service = CreateService();

        // Act
        await WithScopeAsync(async repo =>
        {
            await repo.AddAsync(service, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        Service? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(service.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(service.Id, found!.Id);
        Assert.Equal(service.Code, found.Code);
        Assert.Equal(service.Name, found.Name);
        Assert.Equal(service.Description, found.Description);
        Assert.Equal(service.BasePrice, found.BasePrice);
        Assert.Equal(service.EstimatedDuration, found.EstimatedDuration);
        Assert.Equal(service.IsActive, found.IsActive);
    }

    [Fact(DisplayName = "ServiceRepository >> Should return null >> When service does not exist")]
    public async Task ServiceRepository_ShouldReturnNull_WhenServiceDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        Service? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(id, CancellationToken.None));

        // Assert
        Assert.Null(found);
    }

    [Fact(DisplayName = "ServiceRepository >> Should persist changes >> When updating an existing service")]
    public async Task ServiceRepository_ShouldPersistChanges_WhenUpdatingExistingService()
    {
        // Arrange
        var service = CreateService();
        await SeedAsync(service);

        var updated = new Service(
            service.Id, service.Code, "Updated Service", service.Description, service.BasePrice,
            service.EstimatedDuration, false, service.CreatedAt, DateTime.UtcNow);

        // Act
        await WithScopeAsync(async repo =>
        {
            repo.Update(updated);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        Service? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(service.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal("Updated Service", found!.Name);
        Assert.False(found.IsActive);
    }

    [Fact(DisplayName = "ServiceRepository >> Should remove entity >> When removing an existing service")]
    public async Task ServiceRepository_ShouldRemoveEntity_WhenRemovingExistingService()
    {
        // Arrange
        var service = CreateService();
        await SeedAsync(service);

        // Act
        await WithScopeAsync(async repo =>
        {
            repo.Remove(service);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        Service? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(service.Id, CancellationToken.None));

        Assert.Null(found);
    }
}
