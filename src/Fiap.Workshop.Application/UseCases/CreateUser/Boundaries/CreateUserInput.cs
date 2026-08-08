using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Application.UseCases.CreateUser.Boundaries;

public sealed record CreateUserInput(
    Guid CorrelationId,
    string Name,
    string Email,
    string Password,
    UserRole Role
);
