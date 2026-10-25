using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.Users.LoginUser.Mapper;

public static class LoginUserMapper
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
