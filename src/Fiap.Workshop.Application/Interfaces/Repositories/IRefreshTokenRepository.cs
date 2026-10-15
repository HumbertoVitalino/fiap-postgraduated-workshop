using Fiap.Workshop.Application.Abstractions;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RefreshToken>> GetAllActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
