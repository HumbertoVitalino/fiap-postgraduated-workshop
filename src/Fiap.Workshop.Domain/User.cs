using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Users;
using Fiap.Workshop.Domain.Users.Events;

namespace Fiap.Workshop.Domain;

public sealed class User : AggregateRoot
{
    public const int EmailMaxLength = 256;

    public string Name { get; private set; }
    public HashedPassword Password { get; private set; }
    public UserRole Role { get; private set; }
    public string Email
    {
        get;
        private set => field = value.Trim().ToLowerInvariant();
    }

    private User(
        Guid id,
        string email,
        string name,
        HashedPassword password,
        UserRole role,
        DateTime createdAt,
        DateTime updatedAt
    ) : base(id, createdAt, updatedAt)
    {
        Email = email;
        Name = name;
        Password = password;
        Role = role;
    }

    public static User Create(
        string email,
        string name,
        HashedPassword password,
        UserRole role = UserRole.User
    )
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new Exception(UserErrors.NameEmpty);

        var user = new User(
            Guid.NewGuid(),
            email,
            name,
            password,
            role,
            DateTime.Now,
            DateTime.Now
        );

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id));

        return user;
    }

    public static User Rehydrate(
        Guid id,
        string email,
        string name,
        string passwordHash,
        UserRole role,
        DateTime createdAt,
        DateTime updatedAt
    )
    {
        return new(
            id,
            email,
            name,
            HashedPassword.FromHash(passwordHash),
            role,
            createdAt,
            updatedAt
        );
    }

    public bool VerifyPassword(string rawPassword, IPasswordHasher passwordHasher) =>
        Password.Matches(rawPassword, passwordHasher);

    public void UpdateEmail(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (normalizedEmail == Email)
            return;

        Email = normalizedEmail;
        Touch();
    }
}
