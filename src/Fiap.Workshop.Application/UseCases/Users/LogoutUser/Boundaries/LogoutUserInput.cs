namespace Fiap.Workshop.Application.UseCases.Users.LogoutUser.Boundaries;

public sealed record LogoutUserInput(
    Guid CorrelationId,
    string RefreshToken
);
