namespace Fiap.Workshop.Application.UseCases.Users.DeleteUser.Boundaries;

public readonly struct DeleteUserInput(
    Guid correlationId,
    Guid userId
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid UserId { get; init; } = userId;
}
