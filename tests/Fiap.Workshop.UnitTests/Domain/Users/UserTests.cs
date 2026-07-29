using Fiap.Workshop.Domain;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Users;
using Fiap.Workshop.Domain.Users.Events;
using FluentAssertions;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain.Users;

public sealed class UserTests
{
    private const string ValidPassword = "ValidPass123";

    private readonly FakePasswordHasher _passwordHasher = new();

    [Fact(DisplayName = "Create >> Should Return User With Correct Properties >> When Input Is Valid")]
    public void Create_ValidData_ReturnsUserWithCorrectProperties()
    {
        // Arrange
        const string name = "John Doe";
        const string email = "john@example.com";
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);

        // Act
        var user = User.Create(email, name, password);

        // Assert
        user.Id.Should().NotBeEmpty();
        user.Name.Should().Be(name);
        user.Email.Should().Be(email);
        user.Password.Should().Be(password);
    }

    [Fact(DisplayName = "Create >> Should Return User With Normalized Email >> When Email Has Whitespace Or Mixed Case")]
    public void Create_EmailWithWhitespaceOrMixedCase_ReturnsUserWithNormalizedEmail()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);

        // Act
        var user = User.Create("  John@Example.COM  ", "John Doe", password);

        // Assert
        user.Email.Should().Be("john@example.com");
    }

    [Fact(DisplayName = "Create >> Should Raise UserCreatedEvent >> When User Is Created")]
    public void Create_ValidData_RaisesUserCreatedEvent()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);

        // Act
        var user = User.Create("john@example.com", "John Doe", password);

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
        // Arrange
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);

        // Act
        var act = () => User.Create("john@example.com", name!, password);

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.NameEmpty);
    }

    [Fact(DisplayName = "UpdateEmail >> Should Change Email >> When New Email Is Valid")]
    public void UpdateEmail_ValidEmail_ChangesEmail()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);
        var user = User.Create("old@example.com", "John Doe", password);
        const string newEmail = "new@example.com";

        // Act
        user.UpdateEmail(newEmail);

        // Assert
        user.Email.Should().Be(newEmail);
    }

    [Fact(DisplayName = "UpdateEmail >> Should Normalize Email >> When New Email Has Whitespace Or Mixed Case")]
    public void UpdateEmail_EmailWithWhitespaceOrMixedCase_NormalizesEmail()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);
        var user = User.Create("old@example.com", "John Doe", password);

        // Act
        user.UpdateEmail("  New@Example.COM  ");

        // Assert
        user.Email.Should().Be("new@example.com");
    }

    [Fact(DisplayName = "ClearDomainEvents >> Should Remove All Events >> When Called")]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);
        var user = User.Create("john@example.com", "John Doe", password);

        // Act
        user.ClearDomainEvents();

        // Assert
        user.GetDomainEvents().Should().BeEmpty();
    }

    [Fact(DisplayName = "VerifyPassword >> Should Return True >> When Raw Password Matches Hash")]
    public void VerifyPassword_MatchingPassword_ReturnsTrue()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);
        var user = User.Create("john@example.com", "John Doe", password);

        // Act & Assert
        user.VerifyPassword(ValidPassword, _passwordHasher).Should().BeTrue();
    }

    [Fact(DisplayName = "VerifyPassword >> Should Return False >> When Raw Password Does Not Match Hash")]
    public void VerifyPassword_NonMatchingPassword_ReturnsFalse()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);
        var user = User.Create("john@example.com", "John Doe", password);

        // Act & Assert
        user.VerifyPassword("WrongPass123", _passwordHasher).Should().BeFalse();
    }

    [Fact(DisplayName = "Rehydrate >> Should Return User Without Raising Events >> When Called")]
    public void Rehydrate_ValidData_ReturnsUserWithoutRaisingEvents()
    {
        // Arrange
        var id = Guid.NewGuid();
        var passwordHash = _passwordHasher.Hash(ValidPassword);
        var createdAt = DateTime.UtcNow.AddDays(-10);
        var updatedAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var user = User.Rehydrate(id, "john@example.com", "John Doe", passwordHash, UserRole.Admin, createdAt, updatedAt);

        // Assert
        user.Id.Should().Be(id);
        user.Role.Should().Be(UserRole.Admin);
        user.CreatedAt.Should().Be(createdAt);
        user.UpdatedAt.Should().Be(updatedAt);
        user.GetDomainEvents().Should().BeEmpty();
        user.VerifyPassword(ValidPassword, _passwordHasher).Should().BeTrue();
    }

    [Fact(DisplayName = "Create >> Should Set CreatedAt And Leave UpdatedAt Null >> When User Is Created")]
    public void Create_ValidData_SetsCreatedAtAndLeavesUpdatedAtNull()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);
        var before = DateTime.UtcNow;

        // Act
        var user = User.Create("john@example.com", "John Doe", password);

        // Assert
        user.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        user.UpdatedAt.Should().Be(before);
    }

    [Fact(DisplayName = "UpdateEmail >> Should Set UpdatedAt >> When Called")]
    public void UpdateEmail_ValidEmail_SetsUpdatedAt()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);
        var user = User.Create("old@example.com", "John Doe", password);

        // Act
        user.UpdateEmail("new@example.com");

        // Assert
        user.UpdatedAt.Should().NotBe(null);
        user.UpdatedAt!.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact(DisplayName = "UpdateEmail >> Should Not Touch UpdatedAt >> When New Email Is The Same As Current")]
    public void UpdateEmail_SameEmail_DoesNotSetUpdatedAt()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw(ValidPassword, _passwordHasher);
        var user = User.Create("john@example.com", "John Doe", password);

        // Act
        user.UpdateEmail("  John@Example.COM  ");

        // Assert
        user.UpdatedAt.Should().BeNull();
    }
}
