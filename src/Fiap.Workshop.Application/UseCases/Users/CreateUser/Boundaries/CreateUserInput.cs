using Fiap.Workshop.Domain.Users;

namespace Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;

public sealed class CreateUserInput(
    Guid correlationId,
    string name,
    string email,
    string password,
    UserRole role
)
{
    public Guid CorrelationId { get; private set; } = correlationId;
    public string Name { get; private set; } = name;
    public string Password { get; private set; } = password;
    public UserRole Role { get; private set; } = role;

    public string Email
    {
        get;
        private set => field = value.Trim().ToLowerInvariant();
    } = email;
}
