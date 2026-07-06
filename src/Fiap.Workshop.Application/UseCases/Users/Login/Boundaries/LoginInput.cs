namespace Fiap.Workshop.Application.UseCases.Users.Login.Boundaries;

public sealed record LoginInput(string Email, string Password, Guid CorrelationId = default);
