using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Domain.Users;

namespace Fiap.Workshop.Application.UseCases.Users.CreateUser.Mapper;

internal static class CreateUserMapper
{
    internal static UserResponse MapToOutput(this User user) =>
        new(user.Id, user.Name, user.Email.Value, user.Role.ToString());
}
