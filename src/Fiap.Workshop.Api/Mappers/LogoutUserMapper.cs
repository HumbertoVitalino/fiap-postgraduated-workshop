using Fiap.Workshop.Api.Requests.Auth;
using Fiap.Workshop.Application.UseCases.Users.LogoutUser.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class LogoutUserMapper
{
    public static LogoutUserInput MapToInput(this LogoutRequest request)
    {
        return new(
            request.CorrelationId,
            request.RefreshToken
        );
    }
}
