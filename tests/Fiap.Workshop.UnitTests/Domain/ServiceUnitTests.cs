using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class ServiceUnitTests
{
    private readonly Fixture _fixture = new();

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
}
