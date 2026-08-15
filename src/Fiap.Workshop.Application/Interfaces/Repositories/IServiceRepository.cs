using Fiap.Workshop.Application.Abstractions;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.Interfaces.Repositories;

public interface IServiceRepository : IRepository<Service>
{
    Task<bool> ExistsWithCodeAsync(string code, CancellationToken cancellationToken);
    Task<IEnumerable<Service>> GetAllAsync(CancellationToken cancellationToken);
}
