using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Models;

namespace Fiap.Workshop.Infrastructure.Repositories.Mappers;

internal static class ModelMappers
{
    internal static UserModel MapToModel(this User user)
    {
        return new(
            user.Id,
            user.Email,
            user.Name,
            user.Password,
            user.Role,
            user.CreatedAt,
            user.UpdatedAt
        );
    }
}
