namespace Fiap.Workshop.Application.UseCases.Users.UpdateEmail.Boundaries;

public sealed record UpdateEmailInput(
    Guid CorrelationId,
    Guid UserId,
    string Email
);
