namespace Fiap.Workshop.Application.UseCases.Users.Login.Boundaries;

public sealed class LoginInput(
    Guid correlationId,
    string email,
    string password
)
{
    public Guid CorrelationId { get; init; } = correlationId;

    public string Email
    {
        get;
        init => field = value.Trim().ToLowerInvariant();
    } = email;

    public string Password { get; init; } = password;
}
