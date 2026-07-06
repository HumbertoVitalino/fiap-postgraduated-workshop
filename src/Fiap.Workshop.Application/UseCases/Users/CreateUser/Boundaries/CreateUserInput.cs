using Fiap.Workshop.Domain.Users;

namespace Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;

public sealed record CreateUserInput(
    Guid CorrelationId,
    string Name,
    string Email,
    string Password,
    UserRole Role
);
