using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class CustomerUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "Customer >> Should be created >> When all required properties are provided")]
    public void Customer_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = _fixture.Create<string>();
        var document = _fixture.Create<string>();
        var email = _fixture.Create<string>();
        var phone = _fixture.Create<string>();
        var createdAt = DateTime.Now;
        var updatedAt = DateTime.Now;

        // Act
        var customer = new Customer(
            id,
            name,
            document,
            email,
            phone,
            createdAt,
            updatedAt
        );

        // Assert
        Assert.Equal(id, customer.Id);
        Assert.Equal(name, customer.Name);
        Assert.Equal(document, customer.Document);
        Assert.Equal(email, customer.Email);
        Assert.Equal(phone, customer.Phone);
        Assert.Equal(createdAt, customer.CreatedAt);
        Assert.Equal(updatedAt, customer.UpdatedAt);
    }
}
