namespace Fiap.Workshop.Api.Requests.Users;

public sealed record ChangePasswordRequest(
    Guid CorrelationId,
    string CurrentPassword,
    string NewPassword
);
