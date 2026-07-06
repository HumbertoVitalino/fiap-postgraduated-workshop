using Fiap.Workshop.Domain.Users;

namespace Fiap.Workshop.Api.Requests.Users;

public sealed record CreateUserRequest(
    string Name, 
    string Email,
    string Password,
    string PasswordConfirmation,
    UserRole Role
);
