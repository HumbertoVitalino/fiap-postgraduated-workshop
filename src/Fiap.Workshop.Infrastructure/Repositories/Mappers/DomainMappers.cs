using Fiap.Workshop.Domain;
using Fiap.Workshop.Domain.Users;
using Fiap.Workshop.Infrastructure.Repositories.Models;

namespace Fiap.Workshop.Infrastructure.Repositories.Mappers;

internal static class DomainMappers
{
    internal static User MapToDomain(this UserModel model) =>
        User.Rehydrate(model.Id, model.Email, model.Name, model.Password, Enum.Parse<UserRole>(model.Role), model.CreatedAt, model.UpdatedAt);
}
