using Fiap.Workshop.Api.Requests.Auth;
using Fiap.Workshop.Application.UseCases.Users.Login.Boundaries;

namespace Fiap.Workshop.Api.Mappers.Auth;

internal static class LoginRequestMapper
{
    internal static LoginInput MapToInput(this LoginRequest request, Guid correlationId)
        => new(
            correlationId,
            request.Email,
            request.Password
        );
}
