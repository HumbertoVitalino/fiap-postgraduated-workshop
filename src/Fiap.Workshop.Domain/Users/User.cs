using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Users.Events;

namespace Fiap.Workshop.Domain.Users;

public sealed class User : AggregateRoot<Guid>
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
        UserRole role
    ) : base(id)
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
            throw new DomainException(UserErrors.NameEmpty);

        var user = new User(Guid.NewGuid(), email, name, password, role);
        user.RaiseDomainEvent(new UserCreatedEvent(user.Id));

        return user;
    }

    public static User Rehydrate(Guid id, string email, string name, string passwordHash, UserRole role) =>
        new(id, email, name, HashedPassword.FromHash(passwordHash), role);

    public bool VerifyPassword(string rawPassword, IPasswordHasher passwordHasher) =>
        Password.Matches(rawPassword, passwordHasher);

    public void UpdateEmail(string email) => Email = email;
}
