using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Application.UseCases.CreateUser.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class CreateUserMapper
{
    public static CreateUserInput MapToInput(this CreateUserRequest request)
    {
        return new(
            request.CorrelationId,
            request.Name,
            request.Email,
            request.Password,
            request.Role
        );
    }
}
