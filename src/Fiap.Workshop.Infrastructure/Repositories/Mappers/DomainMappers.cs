using Fiap.Workshop.Domain.Users;
using Fiap.Workshop.Infrastructure.Repositories.Models;

namespace Fiap.Workshop.Infrastructure.Repositories.Mappers;

internal static class DomainMappers
{
    internal static User MapToDomain(this UserModel model) =>
        new(model.Id, new Email(model.Email), model.Name, Enum.Parse<UserRole>(model.Role));
}
