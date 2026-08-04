using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Xunit;

namespace Fiap.Workshop.UnitTests.Infrastructure.Mappers;

public class ServiceMapperUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "Service >> Should map to model >> When mapping from domain")]
    public void Service_ShouldMapToModel_WhenMappingFromDomain()
    {
        // Arrange
        var service = new Service(
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            _fixture.Create<short>(),
            true,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var model = service.MapToModel();

        // Assert
        Assert.Equal(service.Id, model.Id);
        Assert.Equal(service.Code, model.Code);
        Assert.Equal(service.Name, model.Name);
        Assert.Equal(service.Description, model.Description);
        Assert.Equal(service.BasePrice, model.BasePrice);
        Assert.Equal(service.EstimatedDuration, model.EstimatedDuration);
        Assert.Equal(service.IsActive, model.IsActive);
        Assert.Equal(service.CreatedAt, model.CreatedAt);
        Assert.Equal(service.UpdatedAt, model.UpdatedAt);
    }

    [Fact(DisplayName = "ServiceModel >> Should map to domain >> When mapping from model")]
    public void ServiceModel_ShouldMapToDomain_WhenMappingFromModel()
    {
        // Arrange
        var model = new ServiceModel(
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<decimal>(),
            _fixture.Create<short>(),
            false,
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var service = model.MapToDomain();

        // Assert
        Assert.Equal(model.Id, service.Id);
        Assert.Equal(model.Code, service.Code);
        Assert.Equal(model.Name, service.Name);
        Assert.Equal(model.Description, service.Description);
        Assert.Equal(model.BasePrice, service.BasePrice);
        Assert.Equal(model.EstimatedDuration, service.EstimatedDuration);
        Assert.Equal(model.IsActive, service.IsActive);
        Assert.Equal(model.CreatedAt, service.CreatedAt);
        Assert.Equal(model.UpdatedAt, service.UpdatedAt);
    }
}
