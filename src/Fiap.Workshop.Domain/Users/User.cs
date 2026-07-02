using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Users.Events;

namespace Fiap.Workshop.Domain.Users;

public sealed class User : AggregateRoot<Guid>
{
    internal User(Guid id, Email email, string name, UserRole role) : base(id)
    {
        Email = email;
        Name = name;
        Role = role;
    }

    public Email Email { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public UserRole Role { get; private set; }

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
