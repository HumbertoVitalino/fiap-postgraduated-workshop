using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Users.Events;

namespace Fiap.Workshop.Domain.Users;

public sealed class User : AggregateRoot<Guid>
{
    public Email Email { get; private set; }
    public string Name { get; private set; }
    public string Password { get; private set; }
    public UserRole Role { get; private set; }

    public User(
        Guid id,
        string email,
        string name,
        string password,
        UserRole role = UserRole.User
    ) : base(id)
    {
        Email = Email.Create(email);
        Name = name;
        Password = password;
        Role = role;

        this.RaiseDomainEvent(new UserCreatedEvent(id));
    }

    public void UpdateEmail(string email)
    {
        Email = Email.Create(email);
    }
}
