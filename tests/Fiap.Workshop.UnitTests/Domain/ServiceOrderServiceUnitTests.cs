using AutoFixture;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Errors;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class ServiceOrderServiceUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "ServiceOrderService >> Should be created >> When all required properties are provided")]
    public void ServiceOrderService_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var serviceOrderId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var name = _fixture.Create<string>();
        var description = _fixture.Create<string>();
        var unitPrice = _fixture.Create<decimal>();
        var estimatedDuration = _fixture.Create<short>();
        var quantity = _fixture.Create<int>();
        var createdAt = DateTime.Now;
        var updatedAt = DateTime.Now;

        // Act
        var service = new ServiceOrderService(
            id,
            serviceOrderId,
            serviceId,
            name,
            description,
            unitPrice,
            estimatedDuration,
            quantity,
            createdAt,
            updatedAt
        );

        // Assert
        Assert.Equal(id, service.Id);
        Assert.Equal(serviceOrderId, service.ServiceOrderId);
        Assert.Equal(serviceId, service.ServiceId);
        Assert.Equal(name, service.Name);
        Assert.Equal(description, service.Description);
        Assert.Equal(unitPrice, service.UnitPrice);
        Assert.Equal(estimatedDuration, service.EstimatedDuration);
        Assert.Equal(quantity, service.Quantity);
        Assert.Equal(createdAt, service.CreatedAt);
        Assert.Equal(updatedAt, service.UpdatedAt);
    }

    [Theory(DisplayName = "ServiceOrderService >> Should throw >> When quantity is not greater than zero")]
    [InlineData(0)]
    [InlineData(-1)]
    public void ServiceOrderService_ShouldThrow_WhenQuantityIsNotGreaterThanZero(int quantity)
    {
        // Act
        ServiceOrderService act() => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            _fixture.Create<short>(),
            quantity,
            DateTime.Now,
            DateTime.Now
        );

        // Assert
        var exception = Assert.Throws<DomainException>((Func<ServiceOrderService>)act);
        Assert.Equal(ServiceOrderErrors.InvalidQuantity, exception.Message);
    }

    [Fact(DisplayName = "ServiceOrderService >> Should throw >> When unit price is negative")]
    public void ServiceOrderService_ShouldThrow_WhenUnitPriceIsNegative()
    {
        // Act
        ServiceOrderService act() => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            -1,
            _fixture.Create<short>(),
            _fixture.Create<int>(),
            DateTime.Now,
            DateTime.Now
        );

        // Assert
        var exception = Assert.Throws<DomainException>((Func<ServiceOrderService>)act);
        Assert.Equal(ServiceOrderErrors.InvalidUnitPrice, exception.Message);
    }

    [Fact(DisplayName = "ServiceOrderService >> Should throw >> When estimated duration is negative")]
    public void ServiceOrderService_ShouldThrow_WhenEstimatedDurationIsNegative()
    {
        // Act
        var act = () => new ServiceOrderService(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            -1,
            _fixture.Create<int>(),
            DateTime.Now,
            DateTime.Now
        );

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(ServiceOrderErrors.InvalidEstimatedDuration, exception.Message);
    }
}
