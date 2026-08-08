using Fiap.Workshop.Application.UseCases.CreateUser.Boundaries;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.UseCases.CreateUser.Mapper;

public static class CreateUserMapper
{
    public static User MapToDomain(this CreateUserInput input, string passwordHash)
    {
        return new(
            input.CorrelationId,
            input.Email,
            input.Name,
            passwordHash,
            input.Role,
            DateTime.Now,
            DateTime.Now
        );
    }
}
