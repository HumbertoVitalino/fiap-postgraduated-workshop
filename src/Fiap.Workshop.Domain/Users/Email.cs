using Fiap.Workshop.Domain.Abstractions;

namespace Fiap.Workshop.Domain.Users;

public sealed class Email : ValueObject
{
    public const int MaxLength = 256;

    public string Value { get; }

    internal Email(string value) => Value = value;

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException(UserErrors.EmailEmpty);

        if (email.Length > MaxLength)
            throw new DomainException(UserErrors.EmailTooLong);

        if (!email.Contains('@'))
            throw new DomainException(UserErrors.EmailInvalidFormat);

        return new Email(email.Trim().ToLowerInvariant());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
