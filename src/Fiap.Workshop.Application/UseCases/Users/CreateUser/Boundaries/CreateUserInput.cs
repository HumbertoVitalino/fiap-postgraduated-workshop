using Fiap.Workshop.Domain.Users;

namespace Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;

public sealed record CreateUserInput(string Name, string Email, Guid CorrelationId = default, UserRole Role = UserRole.User);
