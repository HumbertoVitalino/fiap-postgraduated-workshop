namespace Fiap.Workshop.Application.DTOs.Users;

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role
);
