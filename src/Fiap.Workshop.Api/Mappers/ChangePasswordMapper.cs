using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Application.UseCases.Users.ChangePassword.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class ChangePasswordMapper
{
    public static ChangePasswordInput MapToInput(this ChangePasswordRequest request, Guid userId)
    {
        return new(
            request.CorrelationId,
            userId,
            request.CurrentPassword,
            request.NewPassword
        );
    }
}
