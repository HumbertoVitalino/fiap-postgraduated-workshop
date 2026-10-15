namespace Fiap.Workshop.Application.DTOs.Users;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken
);
