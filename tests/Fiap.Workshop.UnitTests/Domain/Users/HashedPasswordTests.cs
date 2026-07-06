using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Users;
using FluentAssertions;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain.Users;

public sealed class HashedPasswordTests
{
    private readonly FakePasswordHasher _passwordHasher = new();

    [Fact(DisplayName = "CreateFromRaw >> Should Return Hashed Value >> When Password Is Valid")]
    public void CreateFromRaw_ValidPassword_ReturnsHashedValue()
    {
        // Act
        var password = HashedPassword.CreateFromRaw("ValidPass123", _passwordHasher);

        // Assert
        password.Value.Should().Be(_passwordHasher.Hash("ValidPass123"));
    }

    [Theory(DisplayName = "CreateFromRaw >> Should Throw DomainException >> When Password Is Empty Or Whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateFromRaw_EmptyPassword_ThrowsDomainException(string? rawPassword)
    {
        // Act
        var act = () => HashedPassword.CreateFromRaw(rawPassword!, _passwordHasher);

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.PasswordEmpty);
    }

    [Fact(DisplayName = "CreateFromRaw >> Should Throw DomainException >> When Password Is Too Short")]
    public void CreateFromRaw_TooShort_ThrowsDomainException()
    {
        // Act
        var act = () => HashedPassword.CreateFromRaw("Short1A", _passwordHasher);

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.PasswordTooShort);
    }

    [Fact(DisplayName = "CreateFromRaw >> Should Throw DomainException >> When Password Has No Uppercase Letter")]
    public void CreateFromRaw_MissingUppercase_ThrowsDomainException()
    {
        // Act
        var act = () => HashedPassword.CreateFromRaw("lowercase123", _passwordHasher);

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.PasswordMissingUppercase);
    }

    [Fact(DisplayName = "CreateFromRaw >> Should Throw DomainException >> When Password Has No Lowercase Letter")]
    public void CreateFromRaw_MissingLowercase_ThrowsDomainException()
    {
        // Act
        var act = () => HashedPassword.CreateFromRaw("UPPERCASE123", _passwordHasher);

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.PasswordMissingLowercase);
    }

    [Fact(DisplayName = "CreateFromRaw >> Should Throw DomainException >> When Password Has No Digit")]
    public void CreateFromRaw_MissingDigit_ThrowsDomainException()
    {
        // Act
        var act = () => HashedPassword.CreateFromRaw("NoDigitsHere", _passwordHasher);

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.PasswordMissingDigit);
    }

    [Fact(DisplayName = "FromHash >> Should Return HashedPassword >> When Hash Is Valid")]
    public void FromHash_ValidHash_ReturnsHashedPassword()
    {
        // Act
        var password = HashedPassword.FromHash("some-hash-value");

        // Assert
        password.Value.Should().Be("some-hash-value");
    }

    [Theory(DisplayName = "FromHash >> Should Throw DomainException >> When Hash Is Empty Or Whitespace")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FromHash_EmptyHash_ThrowsDomainException(string? hash)
    {
        // Act
        var act = () => HashedPassword.FromHash(hash!);

        // Assert
        act.Should().Throw<DomainException>().WithMessage(UserErrors.PasswordHashEmpty);
    }

    [Fact(DisplayName = "Matches >> Should Return True >> When Raw Password Matches Hash")]
    public void Matches_MatchingPassword_ReturnsTrue()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw("ValidPass123", _passwordHasher);

        // Act & Assert
        password.Matches("ValidPass123", _passwordHasher).Should().BeTrue();
    }

    [Fact(DisplayName = "Matches >> Should Return False >> When Raw Password Does Not Match Hash")]
    public void Matches_NonMatchingPassword_ReturnsFalse()
    {
        // Arrange
        var password = HashedPassword.CreateFromRaw("ValidPass123", _passwordHasher);

        // Act & Assert
        password.Matches("WrongPass123", _passwordHasher).Should().BeFalse();
    }

    [Fact(DisplayName = "Equals >> Should Return True >> When Hashes Have Same Value")]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var a = HashedPassword.FromHash("same-hash");
        var b = HashedPassword.FromHash("same-hash");

        // Act & Assert
        a.Should().Be(b);
    }
}
