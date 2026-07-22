using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Application.UseCases.Users.UpdateEmail.Boundaries;

namespace Fiap.Workshop.Api.Mappers.Users;

internal static class UpdateEmailRequestMapper
{
    internal static UpdateEmailInput MapToInput(this UpdateEmailRequest request, Guid userId, Guid correlationId)
        => new(correlationId, userId, request.Email);
}
