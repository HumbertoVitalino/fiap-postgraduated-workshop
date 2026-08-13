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
            UserRole.Admin,
            DateTime.Now,
            DateTime.Now
        );

        // Assert
        var domainEvent = Assert.Single(user.GetDomainEvents());
        var userCreatedEvent = Assert.IsType<UserCreatedEvent>(domainEvent);
        Assert.Equal(id, userCreatedEvent.UserId);
    }

    [Fact(DisplayName = "User >> Should update profile >> When UpdateProfile is called")]
    public void User_ShouldUpdateProfile_WhenUpdateProfileIsCalled()
    {
        // Arrange
        var user = new User(
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            UserRole.Admin,
            DateTime.Now,
            DateTime.Now
        );
        user.ClearDomainEvents();

        var newName = _fixture.Create<string>();
        var newEmail = _fixture.Create<string>();
        var newRole = UserRole.Mechanic;
        var before = DateTime.Now;

        // Act
        user.UpdateProfile(newName, newEmail, newRole);

        // Assert
        Assert.Equal(newName, user.Name);
        Assert.Equal(newEmail, user.Email);
        Assert.Equal(newRole, user.Role);
        Assert.InRange(user.UpdatedAt, before, DateTime.Now);
        Assert.Empty(user.GetDomainEvents());
    }

    [Fact(DisplayName = "User >> Should change password >> When ChangePassword is called")]
    public void User_ShouldChangePassword_WhenChangePasswordIsCalled()
    {
        // Arrange
        var user = new User(
            Guid.NewGuid(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            _fixture.Create<string>(),
            UserRole.Admin,
            DateTime.Now,
            DateTime.Now
        );
        user.ClearDomainEvents();

        var newPasswordHash = _fixture.Create<string>();
        var before = DateTime.Now;

        // Act
        user.ChangePassword(newPasswordHash);

        // Assert
        Assert.Equal(newPasswordHash, user.Password);
        Assert.InRange(user.UpdatedAt, before, DateTime.Now);
        Assert.Empty(user.GetDomainEvents());
    }
}
