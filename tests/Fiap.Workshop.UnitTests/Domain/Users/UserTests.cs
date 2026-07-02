using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Users;
using Fiap.Workshop.Domain.Users.Events;
using FluentAssertions;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain.Users;

public sealed class UserTests
{
    [Fact(DisplayName = "Create >> Should Return User With Correct Properties >> When Input Is Valid")]
    public void Create_ValidData_ReturnsUserWithCorrectProperties()
    {
        // Arrange
        const string name = "John Doe";
        const string email = "john@example.com";

        // Act
        var user = User.Create(email, name);

        // Assert
        user.Id.Should().NotBeEmpty();
        user.Name.Should().Be(name);
        user.Email.Value.Should().Be(email);
    }

    [Fact(DisplayName = "Create >> Should Raise UserCreatedEvent >> When User Is Created")]
    public void Create_ValidData_RaisesUserCreatedEvent()
    {
        // Act
        var user = User.Create("john@example.com", "John Doe");

        // Assert
        user.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<UserCreatedEvent>();
    }

    [Theory(DisplayName = "Create >> Should Throw DomainException >> When Name Is Empty Or Whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyName_ThrowsDomainException(string? name)
    {
        // Act
        var act = () => User.Create("john@example.com", name!);

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.NameEmpty);
    }

    [Fact(DisplayName = "Create >> Should Throw DomainException >> When Email Is Invalid")]
    public void Create_InvalidEmail_ThrowsDomainException()
    {
        // Act
        var act = () => User.Create("not-an-email", "John Doe");

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.EmailInvalidFormat);
    }

    [Fact(DisplayName = "UpdateEmail >> Should Change Email >> When New Email Is Valid")]
    public void UpdateEmail_ValidEmail_ChangesEmail()
    {
        // Arrange
        var user = User.Create("old@example.com", "John Doe");
        const string newEmail = "new@example.com";

        // Act
        user.UpdateEmail(newEmail);

        // Assert
        user.Email.Value.Should().Be(newEmail);
    }

    [Fact(DisplayName = "UpdateEmail >> Should Throw DomainException >> When New Email Is Invalid")]
    public void UpdateEmail_InvalidEmail_ThrowsDomainException()
    {
        // Arrange
        var user = User.Create("old@example.com", "John Doe");

        // Act
        var act = () => user.UpdateEmail("invalid");

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.EmailInvalidFormat);
    }

    [Fact(DisplayName = "ClearDomainEvents >> Should Remove All Events >> When Called")]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var user = User.Create("john@example.com", "John Doe");

        // Act
        user.ClearDomainEvents();

        // Assert
        user.GetDomainEvents().Should().BeEmpty();
    }
}
