using Fiap.Workshop.Domain.Abstractions;

namespace Fiap.Workshop.Domain.Users;

public sealed class HashedPassword : ValueObject
{
    public const int MinLength = 10;

    public string Value { get; private set; }

    private HashedPassword(string value)
    {
        Value = value;
    }

    public static HashedPassword CreateFromRaw(string rawPassword, IPasswordHasher passwordHasher)
    {
        EnsureStrengthPolicy(rawPassword);

        return new HashedPassword(passwordHasher.Hash(rawPassword));
    }

    public static HashedPassword FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new DomainException(UserErrors.PasswordHashEmpty);

        return new HashedPassword(hash);
    }

    public bool Matches(string rawPassword, IPasswordHasher passwordHasher) =>
        passwordHasher.Verify(rawPassword, Value);

    private static void EnsureStrengthPolicy(string rawPassword)
    {
        if (string.IsNullOrWhiteSpace(rawPassword))
            throw new DomainException(UserErrors.PasswordEmpty);

        if (rawPassword.Length < MinLength)
            throw new DomainException(UserErrors.PasswordTooShort);

        if (!rawPassword.Any(char.IsUpper))
            throw new DomainException(UserErrors.PasswordMissingUppercase);

        if (!rawPassword.Any(char.IsLower))
            throw new DomainException(UserErrors.PasswordMissingLowercase);

        if (!rawPassword.Any(char.IsDigit))
            throw new DomainException(UserErrors.PasswordMissingDigit);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
