namespace Fiap.Workshop.Api.Requests.Auth;

public sealed record LoginRequest(
    Guid CorrelationId,
    string Email,
    string Password
);