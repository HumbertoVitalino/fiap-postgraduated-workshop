using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Mapper;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.Users.GetUsers.Mapper;

public static class GetUsersMapper
{
    public static IEnumerable<UserResponse> MapToDto(this IEnumerable<User> users) => users.Select(x => x.MapToDto());
}
