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

    [Fact(DisplayName = "Customer >> Should update profile >> When UpdateProfile is called")]
    public void Customer_ShouldUpdateProfile_WhenUpdateProfileIsCalled()
    {
        // Arrange
        var customer = new Customer(
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            DateTime.Now,
            DateTime.Now
        );

        var document = customer.Document;
        var newName = _fixture.Create<string>();
        var newEmail = _fixture.Create<string>();
        var newPhone = _fixture.Create<string>();
        var before = DateTime.Now;

        // Act
        customer.UpdateProfile(newName, newEmail, newPhone);

        // Assert
        Assert.Equal(newName, customer.Name);
        Assert.Equal(newEmail, customer.Email);
        Assert.Equal(newPhone, customer.Phone);
        Assert.Equal(document, customer.Document);
        Assert.InRange(customer.UpdatedAt, before, DateTime.Now);
    }
}
