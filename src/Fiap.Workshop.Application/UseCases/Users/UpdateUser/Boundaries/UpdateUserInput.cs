using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Application.UseCases.Users.UpdateUser.Boundaries;

public sealed class UpdateUserInput(
    Guid correlationId,
    Guid userId,
    string name,
    string email,
    UserRole role
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid UserId { get; init; } = userId;
    public string Name { get; init; } = name;
    public string Email { get; init; } = email.ToLowerInvariant();
    public UserRole Role { get; init; } = role;
}
