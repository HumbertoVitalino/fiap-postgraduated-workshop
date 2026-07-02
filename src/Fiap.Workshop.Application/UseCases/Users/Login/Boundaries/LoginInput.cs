namespace Fiap.Workshop.Application.UseCases.Users.Login.Boundaries;

public sealed record LoginInput(string Email, Guid CorrelationId = default);
