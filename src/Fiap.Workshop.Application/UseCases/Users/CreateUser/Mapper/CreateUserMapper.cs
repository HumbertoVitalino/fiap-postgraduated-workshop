using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Domain.Users;

namespace Fiap.Workshop.Application.UseCases.Users.CreateUser.Mapper;

internal static class CreateUserMapper
{
    internal static User MapToDomain(this CreateUserInput input, IPasswordHasher passwordHasher)
    {
        var password = HashedPassword.CreateFromRaw(input.Password, passwordHasher);

        return User.Create(
            input.Email,
            input.Name,
            password,
            input.Role
        );
    }

    internal static UserResponse MapToOutput(this User user)
    {
        return new(
            user.Id,
            user.Name,
            user.Email,
            user.Role.ToString()
        );
    }
}
