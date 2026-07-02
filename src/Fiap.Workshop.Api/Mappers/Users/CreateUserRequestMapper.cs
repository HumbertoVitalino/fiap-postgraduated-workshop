using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;

namespace Fiap.Workshop.Api.Mappers.Users;

internal static class CreateUserRequestMapper
{
    internal static CreateUserInput MapToInput(this CreateUserRequest request, Guid? correlationId = null) =>
        new(request.Name, request.Email, correlationId ?? Guid.NewGuid());
}
