namespace Fiap.Workshop.Application.UseCases.Users.RefreshAccessToken.Boundaries;

public sealed record RefreshAccessTokenInput(
    Guid CorrelationId,
    string RefreshToken
);
