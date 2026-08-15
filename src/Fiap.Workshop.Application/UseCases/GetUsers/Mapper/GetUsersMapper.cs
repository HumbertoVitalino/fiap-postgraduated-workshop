using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.UseCases.CreateUser.Mapper;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.GetUsers.Mapper;

public static class GetUsersMapper
{
    public static IEnumerable<UserResponse> MapToDto(this IEnumerable<User> users) => users.Select(x => x.MapToDto());
}
