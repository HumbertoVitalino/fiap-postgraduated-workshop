namespace Fiap.Workshop.Infrastructure.Repositories.Models;

public sealed class UserModel
{
    public UserModel() { }

    public UserModel(Guid id, string email, string name, string password, string role, DateTime createdAt, DateTime? updatedAt)
    {
        Id = id;
        Email = email;
        Name = name;
        Password = password;
        Role = role;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Password { get; private set; } = null!;
    public string Role { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}
