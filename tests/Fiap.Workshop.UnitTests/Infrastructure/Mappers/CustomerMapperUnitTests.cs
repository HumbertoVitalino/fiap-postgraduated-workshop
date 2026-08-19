using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Xunit;

namespace Fiap.Workshop.UnitTests.Infrastructure.Mappers;

public class CustomerMapperUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "Customer >> Should map to model >> When mapping from domain")]
    public void Customer_ShouldMapToModel_WhenMappingFromDomain()
    {
        // Arrange
        var customer = new Customer(
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var model = customer.MapToModel();

        // Assert
        Assert.Equal(customer.Id, model.Id);
        Assert.Equal(customer.Name, model.Name);
        Assert.Equal(customer.Document, model.Document);
        Assert.Equal(customer.Email, model.Email);
        Assert.Equal(customer.Phone, model.Phone);
        Assert.Equal(customer.CreatedAt, model.CreatedAt);
        Assert.Equal(customer.UpdatedAt, model.UpdatedAt);
    }

    [Fact(DisplayName = "CustomerModel >> Should map to domain >> When mapping from model")]
    public void CustomerModel_ShouldMapToDomain_WhenMappingFromModel()
    {
        // Arrange
        var model = new CustomerModel(
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        // Act
        var customer = model.MapToDomain();

        // Assert
        Assert.Equal(model.Id, customer.Id);
        Assert.Equal(model.Name, customer.Name);
        Assert.Equal(model.Document, customer.Document);
        Assert.Equal(model.Email, customer.Email);
        Assert.Equal(model.Phone, customer.Phone);
        Assert.Equal(model.CreatedAt, customer.CreatedAt);
        Assert.Equal(model.UpdatedAt, customer.UpdatedAt);
    }
}
