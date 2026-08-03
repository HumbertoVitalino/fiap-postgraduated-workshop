using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Domain.Errors;
using Fiap.Workshop.Domain.Events;

namespace Fiap.Workshop.Domain.Entities;

public sealed class User : AggregateRoot
{
    public string Name { get; private set; }
    public byte[] Password { get; private set; }
    public UserRole Role { get; private set; }
    public string Email { get; private set; }

    public User(
        Guid id,
        string email,
        string name,
        byte[] password,
        UserRole role,
        DateTime createdAt,
        DateTime updatedAt
    ) : base(id, createdAt, updatedAt)
    {
        Email = email;
        Name = name;
        Password = password;
        Role = role;

        RaiseDomainEvent(new UserCreatedEvent(id));
    }
}
