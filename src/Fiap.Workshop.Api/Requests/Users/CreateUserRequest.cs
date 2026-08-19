using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Api.Requests.Users;

public sealed record CreateUserRequest(
    Guid CorrelationId,
    string Name, 
    string Email,
    string Password,
    string PasswordConfirmation,
    UserRole Role
);
