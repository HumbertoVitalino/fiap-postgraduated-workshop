using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class UserModel : Model
{
    public string Email { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Password { get; private set; } = default!;
    public UserRole Role { get; private set; } = default!;

    private UserModel() { }

    public UserModel(
        Guid id,
        string email,
        string name,
        string password,
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
}
