using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Domain;

namespace Fiap.Workshop.Application.UseCases.Users.GetUserById.Mapper;

internal static class GetUserByIdMapper
{
    internal static UserResponse MapToOutput(this User user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString());
}
