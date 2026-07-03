using Fiap.Workshop.Domain.Users;

namespace Fiap.Workshop.Api.Requests.Users;

public sealed record CreateUserRequest(
    string Name, 
    string Email,
    UserRole Role
);
