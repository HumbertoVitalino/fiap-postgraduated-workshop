namespace Fiap.Workshop.Application.Interfaces.Services;

public sealed record GeneratedRefreshToken(
    string Token,
    string TokenHash,
    DateTime ExpiresAt
);
