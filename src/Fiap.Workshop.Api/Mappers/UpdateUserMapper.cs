using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Application.UseCases.UpdateUser.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class UpdateUserMapper
{
    public static UpdateUserInput MapToInput(this UpdateUserRequest request, Guid userId)
    {
        return new(
            request.CorrelationId,
            userId,
            request.Name,
            request.Email,
            request.Role
        );
    }
}
