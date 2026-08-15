namespace Fiap.Workshop.Application.UseCases.Users.LoginUser.Boundaries;

public sealed record LoginUserInput(
    Guid CorrelationId,
    string Email,
    string Password
);