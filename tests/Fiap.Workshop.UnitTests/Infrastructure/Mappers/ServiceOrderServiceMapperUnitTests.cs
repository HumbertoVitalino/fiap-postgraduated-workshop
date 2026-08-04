using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Xunit;

namespace Fiap.Workshop.UnitTests.Infrastructure.Mappers;

public class ServiceOrderServiceMapperUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "ServiceOrderService >> Should map to model >> When mapping from domain")]
    public void ServiceOrderService_ShouldMapToModel_WhenMappingFromDomain()
    {
        // Arrange
        var service = new ServiceOrderService(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            _fixture.Create<short>(),
            _fixture.Create<int>(),
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var model = service.MapToModel();

        // Assert
        Assert.Equal(service.Id, model.Id);
        Assert.Equal(service.ServiceOrderId, model.ServiceOrderId);
        Assert.Equal(service.ServiceId, model.ServiceId);
        Assert.Equal(service.Name, model.Name);
        Assert.Equal(service.Description, model.Description);
        Assert.Equal(service.UnitPrice, model.UnitPrice);
        Assert.Equal(service.Quantity, model.Quantity);
        Assert.Equal(service.EstimatedDuration, model.EstimatedDuration);
        Assert.Equal(service.CreatedAt, model.CreatedAt);
        Assert.Equal(service.UpdatedAt, model.UpdatedAt);
    }

    [Fact(DisplayName = "ServiceOrderServiceModel >> Should map to domain >> When mapping from model")]
    public void ServiceOrderServiceModel_ShouldMapToDomain_WhenMappingFromModel()
    {
        // Arrange
        var model = new ServiceOrderServiceModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            _fixture.Create<short>(),
            _fixture.Create<int>(),
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var service = model.MapToDomain();

        // Assert
        Assert.Equal(model.Id, service.Id);
        Assert.Equal(model.ServiceOrderId, service.ServiceOrderId);
        Assert.Equal(model.ServiceId, service.ServiceId);
        Assert.Equal(model.Name, service.Name);
        Assert.Equal(model.Description, service.Description);
        Assert.Equal(model.UnitPrice, service.UnitPrice);
        Assert.Equal(model.Quantity, service.Quantity);
        Assert.Equal(model.EstimatedDuration, service.EstimatedDuration);
        Assert.Equal(model.CreatedAt, service.CreatedAt);
        Assert.Equal(model.UpdatedAt, service.UpdatedAt);
    }
}
