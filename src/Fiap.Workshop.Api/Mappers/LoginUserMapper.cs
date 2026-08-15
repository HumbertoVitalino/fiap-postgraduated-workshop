using Fiap.Workshop.Api.Requests.Auth;
using Fiap.Workshop.Application.UseCases.Users.LoginUser.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class LoginUserMapper
{
    public static LoginUserInput MapToInput(this LoginRequest request)
    {
        return new(
            request.CorrelationId,
            request.Email,
            request.Password
        );
    }
}
