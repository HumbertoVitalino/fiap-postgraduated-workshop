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
    public Guid CorrelationId { get; init; } = correlationId;
    public string Name { get; init; } = name;
    public string Password { get; init; } = password;
    public UserRole Role { get; init; } = role;

    public string Email
    {
        get;
        init => field = value.Trim().ToLowerInvariant();
    } = email;
}
