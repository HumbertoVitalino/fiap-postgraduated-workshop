using Fiap.Workshop.Domain;
using Fiap.Workshop.Infrastructure.Repositories.Models;

namespace Fiap.Workshop.Infrastructure.Repositories.Mappers;

internal static class ModelMappers
{
    internal static UserModel MapToModel(this User user) =>
        new(user.Id, user.Email, user.Name, user.Password.Value, user.Role.ToString(), user.CreatedAt, user.UpdatedAt);
}
