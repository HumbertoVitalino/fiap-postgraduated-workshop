using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Xunit;

namespace Fiap.Workshop.UnitTests.Infrastructure.Mappers;

public class InventoryItemMapperUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "InventoryItem >> Should map to model >> When mapping from domain")]
    public void InventoryItem_ShouldMapToModel_WhenMappingFromDomain()
    {
        // Arrange
        var inventoryItem = new InventoryItem(
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<int>(),
            _fixture.Create<int>(),
            _fixture.Create<int>(),
            _fixture.Create<decimal>(),
            UnitOfMeasure.Kilogram,
            true,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var model = inventoryItem.MapToModel();

        // Assert
        Assert.Equal(inventoryItem.Id, model.Id);
        Assert.Equal(inventoryItem.Code, model.Code);
        Assert.Equal(inventoryItem.Name, model.Name);
        Assert.Equal(inventoryItem.Description, model.Description);
        Assert.Equal(inventoryItem.QuantityOnHand, model.QuantityOnHand);
        Assert.Equal(inventoryItem.ReservedQuantity, model.ReservedQuantity);
        Assert.Equal(inventoryItem.MinimumStock, model.MinimumStock);
        Assert.Equal(inventoryItem.UnitPrice, model.UnitPrice);
        Assert.Equal(inventoryItem.UnitOfMeasure, model.UnitOfMeasure);
        Assert.Equal(inventoryItem.IsActive, model.IsActive);
        Assert.Equal(inventoryItem.CreatedAt, model.CreatedAt);
        Assert.Equal(inventoryItem.UpdatedAt, model.UpdatedAt);
    }

    [Fact(DisplayName = "InventoryItemModel >> Should map to domain >> When mapping from model")]
    public void InventoryItemModel_ShouldMapToDomain_WhenMappingFromModel()
    {
        // Arrange
        var model = new InventoryItemModel(
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<int>(),
            _fixture.Create<int>(),
            _fixture.Create<int>(),
            _fixture.Create<decimal>(),
            UnitOfMeasure.Liter,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var inventoryItem = model.MapToDomain();

        // Assert
        Assert.Equal(model.Id, inventoryItem.Id);
        Assert.Equal(model.Code, inventoryItem.Code);
        Assert.Equal(model.Name, inventoryItem.Name);
        Assert.Equal(model.Description, inventoryItem.Description);
        Assert.Equal(model.QuantityOnHand, inventoryItem.QuantityOnHand);
        Assert.Equal(model.ReservedQuantity, inventoryItem.ReservedQuantity);
        Assert.Equal(model.MinimumStock, inventoryItem.MinimumStock);
        Assert.Equal(model.UnitPrice, inventoryItem.UnitPrice);
        Assert.Equal(model.UnitOfMeasure, inventoryItem.UnitOfMeasure);
        Assert.Equal(model.IsActive, inventoryItem.IsActive);
        Assert.Equal(model.CreatedAt, inventoryItem.CreatedAt);
        Assert.Equal(model.UpdatedAt, inventoryItem.UpdatedAt);
    }
}
