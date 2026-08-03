using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class InventoryItemUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "InventoryItem >> Should be created >> When all required properties are provided")]
    public void InventoryItem_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var code = _fixture.Create<string>();
        var name = _fixture.Create<string>();
        var description = _fixture.Create<string>();
        var quantityOnHand = _fixture.Create<int>();
        var reservedQuantity = _fixture.Create<int>();
        var minimumStock = _fixture.Create<int>();
        var unitPrice = _fixture.Create<decimal>();
        var unitOfMeasure = UnitOfMeasure.Kilogram;
        var isActive = _fixture.Create<bool>();
        var createdAt = DateTime.Now;
        var updatedAt = DateTime.Now;

        // Act
        var inventoryItem = new InventoryItem(
            id,
            code,
            name,
            description,
            quantityOnHand,
            reservedQuantity,
            minimumStock,
            unitPrice,
            unitOfMeasure,
            isActive,
            createdAt,
            updatedAt
        );

        // Assert
        Assert.Equal(id, inventoryItem.Id);
        Assert.Equal(code, inventoryItem.Code);
        Assert.Equal(name, inventoryItem.Name);
        Assert.Equal(description, inventoryItem.Description);
        Assert.Equal(quantityOnHand, inventoryItem.QuantityOnHand);
        Assert.Equal(reservedQuantity, inventoryItem.ReservedQuantity);
        Assert.Equal(minimumStock, inventoryItem.MinimumStock);
        Assert.Equal(unitPrice, inventoryItem.UnitPrice);
        Assert.Equal(unitOfMeasure, inventoryItem.UnitOfMeasure);
        Assert.Equal(isActive, inventoryItem.IsActive);
        Assert.Equal(createdAt, inventoryItem.CreatedAt);
        Assert.Equal(updatedAt, inventoryItem.UpdatedAt);
    }
}
