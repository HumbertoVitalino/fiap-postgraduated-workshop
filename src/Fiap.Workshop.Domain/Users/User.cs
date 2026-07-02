using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Users.Events;

namespace Fiap.Workshop.Domain.Users;

public sealed class User(
    Guid id,
    Email email,
    string name,
    UserRole role
) : AggregateRoot<Guid>(id)
{
    public Email Email { get; private set; } = email;
    public string Name { get; private set; } = name;
    public UserRole Role { get; private set; } = role;

    public static User Create(string email, string name, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(UserErrors.NameEmpty);

        var userEmail = Email.Create(email);
        var user = new User(Guid.NewGuid(), userEmail, name, role);
        user.RaiseDomainEvent(new UserCreatedEvent(user.Id));

        return user;
    }

    public void UpdateEmail(string email)
    {
        Email = Email.Create(email);
    }
}
