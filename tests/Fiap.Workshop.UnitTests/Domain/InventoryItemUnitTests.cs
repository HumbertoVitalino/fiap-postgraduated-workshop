using AutoFixture;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Domain.Errors;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class InventoryItemUnitTests
{
    private readonly Fixture _fixture = new();

    private InventoryItem CreateInventoryItem(int quantityOnHand, int reservedQuantity, int minimumStock, decimal unitPrice) => new(
        Guid.NewGuid(),
        _fixture.Create<string>(),
        _fixture.Create<string>(),
        _fixture.Create<string>(),
        quantityOnHand,
        reservedQuantity,
        minimumStock,
        unitPrice,
        UnitOfMeasure.Piece,
        true,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "InventoryItem >> Should be created >> When all required properties are provided")]
    public void InventoryItem_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var code = _fixture.Create<string>();
        var name = _fixture.Create<string>();
        var description = _fixture.Create<string>();
        var quantityOnHand = 100;
        var reservedQuantity = 10;
        var minimumStock = 5;
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

    [Fact(DisplayName = "InventoryItem >> Should throw DomainException >> When quantity on hand is negative")]
    public void InventoryItem_ShouldThrowDomainException_WhenQuantityOnHandIsNegative()
    {
        // Act
        var act = () => CreateInventoryItem(-1, 0, 5, 49.90m);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(InventoryItemErrors.InvalidQuantityOnHand, exception.Message);
    }

    [Fact(DisplayName = "InventoryItem >> Should throw DomainException >> When reserved quantity is negative")]
    public void InventoryItem_ShouldThrowDomainException_WhenReservedQuantityIsNegative()
    {
        // Act
        var act = () => CreateInventoryItem(100, -1, 5, 49.90m);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(InventoryItemErrors.InvalidReservedQuantity, exception.Message);
    }

    [Fact(DisplayName = "InventoryItem >> Should throw DomainException >> When reserved quantity exceeds quantity on hand")]
    public void InventoryItem_ShouldThrowDomainException_WhenReservedQuantityExceedsQuantityOnHand()
    {
        // Act
        var act = () => CreateInventoryItem(10, 11, 5, 49.90m);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(InventoryItemErrors.ReservedQuantityExceedsQuantityOnHand, exception.Message);
    }

    [Fact(DisplayName = "InventoryItem >> Should throw DomainException >> When minimum stock is negative")]
    public void InventoryItem_ShouldThrowDomainException_WhenMinimumStockIsNegative()
    {
        // Act
        var act = () => CreateInventoryItem(100, 0, -1, 49.90m);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(InventoryItemErrors.InvalidMinimumStock, exception.Message);
    }

    [Fact(DisplayName = "InventoryItem >> Should throw DomainException >> When unit price is negative")]
    public void InventoryItem_ShouldThrowDomainException_WhenUnitPriceIsNegative()
    {
        // Act
        var act = () => CreateInventoryItem(100, 0, 5, -0.01m);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(InventoryItemErrors.InvalidUnitPrice, exception.Message);
    }

    [Fact(DisplayName = "InventoryItem >> Should reserve quantity >> When enough stock is available")]
    public void InventoryItem_ShouldReserveQuantity_WhenEnoughStockIsAvailable()
    {
        // Arrange
        var inventoryItem = CreateInventoryItem(20, 5, 5, 49.90m);
        var before = DateTime.Now;

        // Act
        inventoryItem.Reserve(10);

        // Assert
        Assert.Equal(15, inventoryItem.ReservedQuantity);
        Assert.Equal(20, inventoryItem.QuantityOnHand);
        Assert.InRange(inventoryItem.UpdatedAt, before, DateTime.Now);
    }

    [Fact(DisplayName = "InventoryItem >> Should throw DomainException >> When reserving more than the available stock")]
    public void InventoryItem_ShouldThrowDomainException_WhenReservingMoreThanTheAvailableStock()
    {
        // Arrange
        var inventoryItem = CreateInventoryItem(20, 15, 5, 49.90m);

        // Act
        var act = () => inventoryItem.Reserve(6);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(InventoryItemErrors.InsufficientStock, exception.Message);
        Assert.Equal(15, inventoryItem.ReservedQuantity);
    }
}
