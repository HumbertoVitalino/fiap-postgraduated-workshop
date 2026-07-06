using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Users;
using FluentAssertions;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain.Users;

public sealed class EmailTests
{
    [Fact(DisplayName = "Create >> Should Return Normalized Email >> When Email Is Valid")]
    public void Create_ValidEmail_ReturnsEmailWithNormalizedValue()
    {
        // Arrange
        var raw = "  User@Example.COM  ";

        // Act
        var email = Email.Create(raw);

        // Assert
        email.Value.Should().Be("user@example.com");
    }

    [Theory(DisplayName = "Create >> Should Throw DomainException >> When Email Is Empty Or Whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespace_ThrowsDomainException(string? value)
    {
        // Act
        var act = () => Email.Create(value!);

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.EmailEmpty);
    }

    [Fact(DisplayName = "Create >> Should Throw DomainException >> When Email Exceeds Max Length")]
    public void Create_ExceedsMaxLength_ThrowsDomainException()
    {
        // Arrange
        var tooLong = new string('a', Email.MaxLength) + "@b.com";

        // Act
        var act = () => Email.Create(tooLong);

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.EmailTooLong);
    }

    [Fact(DisplayName = "Create >> Should Throw DomainException >> When Email Has No At Sign")]
    public void Create_MissingAtSign_ThrowsDomainException()
    {
        // Act
        var act = () => Email.Create("invalidemail.com");

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.EmailInvalidFormat);
    }

    [Fact(DisplayName = "Equals >> Should Return True >> When Emails Have Same Value")]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var a = Email.Create("test@example.com");
        var b = Email.Create("TEST@EXAMPLE.COM");

        // Act & Assert
        a.Should().Be(b);
    }

    [Fact(DisplayName = "Equals >> Should Return False >> When Emails Have Different Values")]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var a = Email.Create("a@example.com");
        var b = Email.Create("b@example.com");

        // Act & Assert
        a.Should().NotBe(b);
    }
}
