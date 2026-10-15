using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.Users.RefreshAccessToken.Mapper;

public static class RefreshAccessTokenMapper
{
    public static RefreshToken MapToDomain(this GeneratedRefreshToken generatedRefreshToken, Guid userId)
    {
        return new(
            Guid.NewGuid(),
            userId,
            generatedRefreshToken.TokenHash,
            generatedRefreshToken.ExpiresAt,
            DateTime.Now,
            DateTime.Now
        );
    }
}
