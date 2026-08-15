using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.UseCases.CreateUser.Boundaries;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.CreateUser.Mapper;

public static class CreateUserMapper
{
    public static User MapToDomain(this CreateUserInput input, string passwordHash)
    {
        return new(
            Guid.NewGuid(),
            input.Email,
            input.Name,
            passwordHash,
            input.Role,
            DateTime.Now,
            DateTime.Now
        );
    }

    public static UserResponse MapToDto(this User user)
    {
        return new(
            user.Id,
            user.Name,
            user.Email,
            user.Role.ToString()
        );
    }
}
