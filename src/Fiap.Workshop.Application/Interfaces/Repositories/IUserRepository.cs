using Fiap.Workshop.Application.Abstractions;
using Fiap.Workshop.Domain.Users;

namespace Fiap.Workshop.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default);
}
