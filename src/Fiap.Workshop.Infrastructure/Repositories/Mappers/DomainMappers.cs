using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Models;

namespace Fiap.Workshop.Infrastructure.Repositories.Mappers;

internal static class DomainMappers
{
    internal static User MapToDomain(this UserModel model)
    {
        return new User(
            model.Id,
            model.Email,
            model.Name,
            model.Password,
            model.Role,
            model.CreatedAt,
            model.UpdatedAt
        );
    }
}
