using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Domain.Events;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class UserUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "User >> Should be created >> When all required properties are provided")]
    public void User_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var email = _fixture.Create<string>();
        var name = _fixture.Create<string>();
        var password = _fixture.Create<string>();
        var role = UserRole.Admin;
        var createdAt = DateTime.Now;
        var updatedAt = DateTime.Now;

        // Act
        var user = new User(
            id,
            email,
            name,
            password,
            role,
            createdAt,
            updatedAt
        );

        // Assert
        Assert.Equal(id, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(name, user.Name);
        Assert.Equal(password, user.Password);
        Assert.Equal(role, user.Role);
        Assert.Equal(createdAt, user.CreatedAt);
        Assert.Equal(updatedAt, user.UpdatedAt);
    }

    [Fact(DisplayName = "User >> Should raise UserCreatedEvent >> When created")]
    public void User_ShouldRaiseUserCreatedEvent_WhenCreated()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var user = new User(
            id,
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            UserRole.User,
            DateTime.Now,
            DateTime.Now
        );

        // Assert
        var domainEvent = Assert.Single(user.GetDomainEvents());
        var userCreatedEvent = Assert.IsType<UserCreatedEvent>(domainEvent);
        Assert.Equal(id, userCreatedEvent.UserId);
    }
}
