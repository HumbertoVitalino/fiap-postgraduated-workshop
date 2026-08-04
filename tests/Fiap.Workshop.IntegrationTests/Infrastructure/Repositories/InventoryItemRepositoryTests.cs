using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Infrastructure.Repositories;

[Collection("Integration")]
public sealed class InventoryItemRepositoryTests(DatabaseFixture fixture)
{
    private static InventoryItem CreateInventoryItem(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        TestData.ShortString(20),
        "Integration Part",
        "Integration test part",
        100,
        10,
        5,
        49.90m,
        UnitOfMeasure.Piece,
        true,
        DateTime.UtcNow,
        DateTime.UtcNow
    );

    private async Task WithScopeAsync(Func<IInventoryItemRepository, Task> action)
    {
        using var scope = fixture.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<IInventoryItemRepository>());
    }

    private async Task SeedAsync(InventoryItem item) => await WithScopeAsync(async repo =>
    {
        await repo.AddAsync(item, CancellationToken.None);
        await repo.UnitOfWork.CommitAsync(CancellationToken.None);
    });

    [Fact(DisplayName = "InventoryItemRepository >> Should persist and retrieve >> When adding a new item")]
    public async Task InventoryItemRepository_ShouldPersistAndRetrieve_WhenAddingNewItem()
    {
        // Arrange
        var item = CreateInventoryItem();

        // Act
        await WithScopeAsync(async repo =>
        {
            await repo.AddAsync(item, CancellationToken.None);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        InventoryItem? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(item.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(item.Id, found!.Id);
        Assert.Equal(item.Code, found.Code);
        Assert.Equal(item.Name, found.Name);
        Assert.Equal(item.Description, found.Description);
        Assert.Equal(item.QuantityOnHand, found.QuantityOnHand);
        Assert.Equal(item.ReservedQuantity, found.ReservedQuantity);
        Assert.Equal(item.MinimumStock, found.MinimumStock);
        Assert.Equal(item.UnitPrice, found.UnitPrice);
        Assert.Equal(item.UnitOfMeasure, found.UnitOfMeasure);
        Assert.Equal(item.IsActive, found.IsActive);
    }

    [Fact(DisplayName = "InventoryItemRepository >> Should return null >> When item does not exist")]
    public async Task InventoryItemRepository_ShouldReturnNull_WhenItemDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        InventoryItem? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(id, CancellationToken.None));

        // Assert
        Assert.Null(found);
    }

    [Fact(DisplayName = "InventoryItemRepository >> Should persist changes >> When updating an existing item")]
    public async Task InventoryItemRepository_ShouldPersistChanges_WhenUpdatingExistingItem()
    {
        // Arrange
        var item = CreateInventoryItem();
        await SeedAsync(item);

        var updated = new InventoryItem(
            item.Id, item.Code, item.Name, item.Description, 50, item.ReservedQuantity, item.MinimumStock,
            item.UnitPrice, item.UnitOfMeasure, false, item.CreatedAt, DateTime.UtcNow);

        // Act
        await WithScopeAsync(async repo =>
        {
            repo.Update(updated);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        InventoryItem? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(item.Id, CancellationToken.None));

        Assert.NotNull(found);
        Assert.Equal(50, found!.QuantityOnHand);
        Assert.False(found.IsActive);
    }

    [Fact(DisplayName = "InventoryItemRepository >> Should remove entity >> When removing an existing item")]
    public async Task InventoryItemRepository_ShouldRemoveEntity_WhenRemovingExistingItem()
    {
        // Arrange
        var item = CreateInventoryItem();
        await SeedAsync(item);

        // Act
        await WithScopeAsync(async repo =>
        {
            repo.Remove(item);
            await repo.UnitOfWork.CommitAsync(CancellationToken.None);
        });

        // Assert
        InventoryItem? found = null;
        await WithScopeAsync(async repo => found = await repo.GetByIdAsync(item.Id, CancellationToken.None));

        Assert.Null(found);
    }
}
