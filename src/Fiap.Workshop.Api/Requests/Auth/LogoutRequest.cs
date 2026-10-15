namespace Fiap.Workshop.Api.Requests.Auth;

public sealed record LogoutRequest(
    Guid CorrelationId,
    string RefreshToken
);
