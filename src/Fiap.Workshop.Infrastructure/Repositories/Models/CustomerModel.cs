namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class CustomerModel : Model
{
    public string Name { get; private set; } = default!;
    public string Document { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string Phone { get; private set; } = default!;

    private CustomerModel() { }

    public CustomerModel(
        Guid id,
        string name,
        string document,
        string email,
        string phone,
        DateTime createdAt,
        DateTime updatedAt
    ) : base(id, createdAt, updatedAt)
    {
        Name = name;
        Document = document;
        Email = email;
        Phone = phone;
    }
}
