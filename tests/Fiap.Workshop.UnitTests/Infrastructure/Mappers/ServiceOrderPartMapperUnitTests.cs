using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Xunit;

namespace Fiap.Workshop.UnitTests.Infrastructure.Mappers;

public class ServiceOrderPartMapperUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "ServiceOrderPart >> Should map to model >> When mapping from domain")]
    public void ServiceOrderPart_ShouldMapToModel_WhenMappingFromDomain()
    {
        // Arrange
        var part = new ServiceOrderPart(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            _fixture.Create<int>(),
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var model = part.MapToModel();

        // Assert
        Assert.Equal(part.Id, model.Id);
        Assert.Equal(part.ServiceOrderId, model.ServiceOrderId);
        Assert.Equal(part.InventoryItemId, model.InventoryItemId);
        Assert.Equal(part.Name, model.Name);
        Assert.Equal(part.Description, model.Description);
        Assert.Equal(part.UnitPrice, model.UnitPrice);
        Assert.Equal(part.Quantity, model.Quantity);
        Assert.Equal(part.CreatedAt, model.CreatedAt);
        Assert.Equal(part.UpdatedAt, model.UpdatedAt);
    }

    [Fact(DisplayName = "ServiceOrderPartModel >> Should map to domain >> When mapping from model")]
    public void ServiceOrderPartModel_ShouldMapToDomain_WhenMappingFromModel()
    {
        // Arrange
        var model = new ServiceOrderPartModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            _fixture.Create<int>(),
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var part = model.MapToDomain();

        // Assert
        Assert.Equal(model.Id, part.Id);
        Assert.Equal(model.ServiceOrderId, part.ServiceOrderId);
        Assert.Equal(model.InventoryItemId, part.InventoryItemId);
        Assert.Equal(model.Name, part.Name);
        Assert.Equal(model.Description, part.Description);
        Assert.Equal(model.UnitPrice, part.UnitPrice);
        Assert.Equal(model.Quantity, part.Quantity);
        Assert.Equal(model.CreatedAt, part.CreatedAt);
        Assert.Equal(model.UpdatedAt, part.UpdatedAt);
    }
}
