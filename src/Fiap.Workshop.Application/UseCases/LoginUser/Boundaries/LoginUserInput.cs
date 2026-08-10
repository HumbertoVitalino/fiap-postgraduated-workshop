namespace Fiap.Workshop.Application.UseCases.LoginUser.Boundaries;

public sealed record LoginUserInput(
    Guid CorrelationId,
    string Email,
    string Password
);