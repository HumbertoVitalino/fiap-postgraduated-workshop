using Fiap.Workshop.Domain.Abstractions;

namespace Fiap.Workshop.Domain.Entities;

public class Customer(
    Guid id,
    string name,
    string document,
    string email,
    string phone,
    DateTime createdAt,
    DateTime updatedAt
) : AggregateRoot(id, createdAt, updatedAt)
{
    public string Name { get; private set; } = name;
    public string Document { get; private set; } = document;
    public string Email { get; private set; } = email;
    public string Phone { get; private set; } = phone;
}
