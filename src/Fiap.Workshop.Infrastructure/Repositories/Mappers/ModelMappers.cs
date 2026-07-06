using Fiap.Workshop.Domain.Users;
using Fiap.Workshop.Infrastructure.Repositories.Models;

namespace Fiap.Workshop.Infrastructure.Repositories.Mappers;

internal static class ModelMappers
{
    internal static UserModel MapToModel(this User user) =>
        new(user.Id, user.Email.Value, user.Name, user.Password.Value, user.Role.ToString());
}
