namespace Fiap.Workshop.Application.UseCases.Users.GetUserById.Boundaries;

public sealed record GetUserByIdInput(Guid Id, Guid CorrelationId = default);
