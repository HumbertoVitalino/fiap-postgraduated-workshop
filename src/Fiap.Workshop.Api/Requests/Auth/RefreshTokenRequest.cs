namespace Fiap.Workshop.Api.Requests.Auth;

public sealed record RefreshTokenRequest(
    Guid CorrelationId,
    string RefreshToken
);
