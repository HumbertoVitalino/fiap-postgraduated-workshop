using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Api.Requests.Users;

public sealed record UpdateUserRequest(
    Guid CorrelationId,
    string Name,
    string Email,
    UserRole Role
);
