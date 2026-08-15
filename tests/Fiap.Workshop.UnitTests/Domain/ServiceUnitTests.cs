using AutoFixture;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Errors;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class ServiceUnitTests
{
    private readonly Fixture _fixture = new();

    private Service CreateService(decimal basePrice, short estimatedDuration) => new(
        Guid.NewGuid(),
        _fixture.Create<string>(),
        _fixture.Create<string>(),
        _fixture.Create<string>(),
        basePrice,
        estimatedDuration,
        true,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Service >> Should be created >> When all required properties are provided")]
    public void Service_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var code = _fixture.Create<string>();
        var name = _fixture.Create<string>();
        var description = _fixture.Create<string>();
        var basePrice = _fixture.Create<decimal>();
        var estimatedDuration = _fixture.Create<short>();
        var isActive = _fixture.Create<bool>();
        var createdAt = DateTime.Now;
        var updatedAt = DateTime.Now;

        // Act
        var service = new Service(
            id,
            code,
            name,
            description,
            basePrice,
            estimatedDuration,
            isActive,
            createdAt,
            updatedAt
        );

        // Assert
        Assert.Equal(id, service.Id);
        Assert.Equal(code, service.Code);
        Assert.Equal(name, service.Name);
        Assert.Equal(description, service.Description);
        Assert.Equal(basePrice, service.BasePrice);
        Assert.Equal(estimatedDuration, service.EstimatedDuration);
        Assert.Equal(isActive, service.IsActive);
        Assert.Equal(createdAt, service.CreatedAt);
        Assert.Equal(updatedAt, service.UpdatedAt);
    }

    [Fact(DisplayName = "Service >> Should throw DomainException >> When base price is negative")]
    public void Service_ShouldThrowDomainException_WhenBasePriceIsNegative()
    {
        // Act
        var act = () => CreateService(-0.01m, 60);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(ServiceErrors.InvalidBasePrice, exception.Message);
    }

    [Fact(DisplayName = "Service >> Should throw DomainException >> When estimated duration is negative")]
    public void Service_ShouldThrowDomainException_WhenEstimatedDurationIsNegative()
    {
        // Act
        var act = () => CreateService(150.00m, -1);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(ServiceErrors.InvalidEstimatedDuration, exception.Message);
    }
}
