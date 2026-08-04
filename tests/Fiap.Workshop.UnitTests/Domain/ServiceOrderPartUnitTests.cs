using AutoFixture;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Errors;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class ServiceOrderPartUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "ServiceOrderPart >> Should be created >> When all required properties are provided")]
    public void ServiceOrderPart_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var serviceOrderId = Guid.NewGuid();
        var inventoryItemId = Guid.NewGuid();
        var name = _fixture.Create<string>();
        var description = _fixture.Create<string>();
        var unitPrice = _fixture.Create<decimal>();
        var quantity = _fixture.Create<int>();
        var createdAt = DateTime.Now;
        var updatedAt = DateTime.Now;

        // Act
        var part = new ServiceOrderPart(
            id,
            serviceOrderId,
            inventoryItemId,
            name,
            description,
            unitPrice,
            quantity,
            createdAt,
            updatedAt
        );

        // Assert
        Assert.Equal(id, part.Id);
        Assert.Equal(serviceOrderId, part.ServiceOrderId);
        Assert.Equal(inventoryItemId, part.InventoryItemId);
        Assert.Equal(name, part.Name);
        Assert.Equal(description, part.Description);
        Assert.Equal(unitPrice, part.UnitPrice);
        Assert.Equal(quantity, part.Quantity);
        Assert.Equal(createdAt, part.CreatedAt);
        Assert.Equal(updatedAt, part.UpdatedAt);
    }

    [Theory(DisplayName = "ServiceOrderPart >> Should throw >> When quantity is not greater than zero")]
    [InlineData(0)]
    [InlineData(-1)]
    public void ServiceOrderPart_ShouldThrow_WhenQuantityIsNotGreaterThanZero(int quantity)
    {
        // Act
        var act = () => new ServiceOrderPart(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            quantity,
            DateTime.Now,
            DateTime.Now
        );

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(ServiceOrderErrors.InvalidQuantity, exception.Message);
    }

    [Fact(DisplayName = "ServiceOrderPart >> Should throw >> When unit price is negative")]
    public void ServiceOrderPart_ShouldThrow_WhenUnitPriceIsNegative()
    {
        // Act
        var act = () => new ServiceOrderPart(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            -1,
            _fixture.Create<int>(),
            DateTime.Now,
            DateTime.Now
        );

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(ServiceOrderErrors.InvalidUnitPrice, exception.Message);
    }
}
