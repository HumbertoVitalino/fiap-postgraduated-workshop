using Fiap.Workshop.Api.Requests.Auth;
using Fiap.Workshop.Application.UseCases.Users.RefreshAccessToken.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class RefreshAccessTokenMapper
{
    public static RefreshAccessTokenInput MapToInput(this RefreshTokenRequest request)
    {
        return new(
            request.CorrelationId,
            request.RefreshToken
        );
    }
}
