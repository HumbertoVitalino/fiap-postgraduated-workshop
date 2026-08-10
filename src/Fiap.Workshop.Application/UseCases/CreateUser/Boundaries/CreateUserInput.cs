using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Application.UseCases.CreateUser.Boundaries;

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
    public string Email { get; init; } = email.ToLowerInvariant();
    public string Password { get; init; } = password;
    public UserRole Role { get; init; } = role;
}
