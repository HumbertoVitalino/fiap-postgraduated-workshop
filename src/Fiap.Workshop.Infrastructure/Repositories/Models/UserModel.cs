namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class UserModel : Model
{
    public string Email { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Password { get; private set; } = default!;
    public string Role { get; private set; } = default!;

    private UserModel() { }

    public UserModel(
        Guid id,
        string email,
        string name,
        string password,
        string role,
        DateTime createdAt,
        DateTime? updatedAt = null
    ) : base(id, createdAt, updatedAt)
    {
        Email = email;
        Name = name;
        Password = password;
        Role = role;
    }
}
