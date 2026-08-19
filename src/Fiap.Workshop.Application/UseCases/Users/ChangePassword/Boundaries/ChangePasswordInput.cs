namespace Fiap.Workshop.Application.UseCases.Users.ChangePassword.Boundaries;

public sealed class ChangePasswordInput(
    Guid correlationId,
    Guid userId,
    string currentPassword,
    string newPassword
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid UserId { get; init; } = userId;
    public string CurrentPassword { get; init; } = currentPassword;
    public string NewPassword { get; init; } = newPassword;
}
